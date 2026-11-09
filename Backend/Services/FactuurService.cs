using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;

namespace Backend.Services;

// Facturen maken: doorlopend nummer per coach, PDF en mail naar coach en rijder.
// Gebruikt door losse facturen (chat) en betaalverzoeken in een groep (één factuur per rijder).
public class FactuurService
{
    private const string FrontendUrl = "https://www.racecoachfinder.nl";
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _email;

    public FactuurService(AppDbContext context, UserManager<ApplicationUser> userManager, IEmailService email)
    {
        _context = context;
        _userManager = userManager;
        _email = email;
    }

    // Volgend nummer van deze coach in dit jaar: F-2026-0001, F-2026-0002, ...
    public async Task<string> VolgendNummerAsync(string coachId)
    {
        var jaar = DateTime.UtcNow.Year;
        var patroon = new Regex($@"^F-{jaar}-(\d+)$");
        var nummers = await _context.Boekingen
            .Where(b => b.CoachGebruikerId == coachId && b.FactuurnummerTekst != null && b.FactuurnummerTekst.StartsWith($"F-{jaar}-"))
            .Select(b => b.FactuurnummerTekst!)
            .ToListAsync();
        var hoogste = nummers.Select(n => patroon.Match(n)).Where(m => m.Success)
            .Select(m => int.TryParse(m.Groups[1].Value, out var x) ? x : 0).DefaultIfEmpty(0).Max();
        return $"F-{jaar}-{hoogste + 1:D4}";
    }

    public Task<bool> NummerAlGebruiktAsync(string coachId, string nummer) =>
        _context.Boekingen.AnyAsync(b => b.CoachGebruikerId == coachId && b.FactuurnummerTekst == nummer);

    // Maakt de boeking, de PDF en verstuurt de mails. Geeft de boeking en eventuele mailfouten terug.
    public async Task<(Boeking boeking, string fouten)> MaakEnVerstuurAsync(
        ApplicationUser coach, ApplicationUser rijder, string omschrijving, decimal bedrag,
        string factuurnummer, int termijn, string? factuurJson)
    {
        var boeking = new Boeking
        {
            CoachGebruikerId = coach.Id,
            RijderGebruikerId = rijder.Id,
            Omschrijving = omschrijving.Trim(),
            Bedrag = bedrag,
            Status = "Openstaand",
            AangemaaktOp = DateTime.UtcNow,
            FactuurnummerTekst = factuurnummer,
            BetalingsTermijn = termijn > 0 ? termijn : 14,
            FactuurJson = factuurJson
        };
        _context.Boekingen.Add(boeking);
        await _context.SaveChangesAsync();

        var coachNaam = coach.Naam ?? "Coach";
        var rijderNaam = rijder.Naam ?? "Rijder";
        var fouten = "";

        // PDF
        byte[]? pdfBytes = null;
        try
        {
            var coachProfiel = await _context.Coaches.FirstOrDefaultAsync(c => c.GebruikerId == coach.Id);
            var factuurData = PdfService.ParseFactuurJson(factuurJson);
            var regels = factuurData?.Regels is { Count: > 0 } r ? r : new List<FactuurRegel>
            {
                new() { Omschrijving = omschrijving, Aantal = 1, Prijs = (double)bedrag, Btw = 0 }
            };
            var (pdf, totaal) = PdfService.GenereerFactuur(
                factuurnummer, DateTime.Now, boeking.BetalingsTermijn, coachNaam,
                coachProfiel?.FactuurAdres, coachProfiel?.FactuurPostcode, coachProfiel?.FactuurStad, coachProfiel?.FactuurLand,
                coachProfiel?.FactuurTelefoon, coach.Email ?? coachNaam, coachProfiel?.KvkNummer, coachProfiel?.BtwNummer,
                rijderNaam, rijder.Email ?? rijderNaam, regels, factuurData?.Notities);
            pdfBytes = pdf;
            boeking.Bedrag = totaal;
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            fouten = $"PDF mislukt: {ex.Message}";
        }

        var pdfNaam = $"Factuur-{factuurnummer}.pdf";
        if (GeldigEmail(coach.Email))
        {
            try
            {
                var taal = await EmailTaal.VoorAsync(_context, coach.Id);
                await _email.VerstuurAsync(coach.Email!, coachNaam, EmailTemplates.FactuurCoachOnderwerp(factuurnummer, taal),
                    EmailTemplates.FactuurCoach(coachNaam, rijderNaam, factuurnummer, boeking.Omschrijving, boeking.Bedrag, taal), pdfBytes, pdfNaam);
            }
            catch (Exception ex) { fouten += (fouten.Length > 0 ? " | " : "") + $"Mail aan coach mislukt: {ex.Message}"; }
        }
        if (GeldigEmail(rijder.Email))
        {
            try
            {
                var taal = await EmailTaal.VoorAsync(_context, rijder.Id);
                var berichtenUrl = $"{FrontendUrl}/berichten.html?partner={Uri.EscapeDataString(coach.Id)}";
                await _email.VerstuurAsync(rijder.Email!, rijderNaam, EmailTemplates.FactuurRijderOnderwerp(factuurnummer, coachNaam, taal),
                    EmailTemplates.FactuurRijder(rijderNaam, coachNaam, factuurnummer, boeking.Omschrijving, boeking.Bedrag, boeking.BetalingsTermijn, berichtenUrl, taal),
                    pdfBytes, pdfNaam);
            }
            catch (Exception ex) { fouten += (fouten.Length > 0 ? " | " : "") + $"Mail aan rijder mislukt: {ex.Message}"; }
        }
        return (boeking, fouten);
    }

    private static bool GeldigEmail(string? e) =>
        !string.IsNullOrEmpty(e) && e.Contains('@') && e.IndexOf('.', e.IndexOf('@')) > 0;
}
