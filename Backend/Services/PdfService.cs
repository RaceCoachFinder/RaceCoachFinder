using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Reflection;
using System.Text.Json;

namespace Backend.Services;

public class FactuurRegel
{
    public string Omschrijving { get; set; } = "";
    public double Aantal { get; set; }
    public double Prijs { get; set; }
    public double Btw { get; set; }
}

public class FactuurData
{
    public List<FactuurRegel> Regels { get; set; } = new();
    public string Notities { get; set; } = "";
    public string Factuurnummer { get; set; } = "";
    public int Betalingstermijn { get; set; } = 14;
}

public static class PdfService
{
    private static readonly string Geel = "#F5C200";
    private static readonly string Zwart = "#111111";
    private static readonly string Grijs = "#666666";
    private static readonly string LichtGrijs = "#F5F5F5";

    private static byte[] LaadLogo()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("Backend.Resources.logo.png");
        if (stream == null) return Array.Empty<byte>();
        var bytes = new byte[stream.Length];
        stream.Read(bytes, 0, bytes.Length);
        return bytes;
    }

    public static (byte[] pdf, decimal totaalMetFee) GenereerFactuur(
        string factuurnummer,
        DateTime datum,
        int betalingsTermijn,
        string coachNaam,
        string? coachAdres,
        string? coachPostcode,
        string? coachStad,
        string? coachLand,
        string? coachTelefoon,
        string coachEmail,
        string? kvkNummer,
        string? btwNummer,
        string rijderNaam,
        string rijderEmail,
        List<FactuurRegel> regels,
        string? notities)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var vervaldatum = datum.AddDays(betalingsTermijn);
        var logoBytes = LaadLogo();

        // Bereken bedragen
        double exclBtw = 0, btwBedrag = 0;
        foreach (var r in regels)
        {
            var sub = r.Aantal * r.Prijs;
            exclBtw += sub;
            btwBedrag += sub * (r.Btw / 100.0);
        }
        var totaal = exclBtw + btwBedrag;

        static string Eur(double v) => $"€ {v:F2}".Replace(".", ",");

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(1.8f, Unit.Centimetre);
                page.MarginVertical(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(9).FontColor(Zwart));

                page.Content().Column(col =>
                {
                    // ── Header ──────────────────────────────────────────
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("FACTUUR")
                                .FontSize(28).Bold().FontColor(Geel);
                        });

                        row.ConstantItem(180).AlignRight().AlignMiddle().Column(c =>
                        {
                            if (logoBytes.Length > 0)
                                c.Item().Width(160).Image(logoBytes);
                            else
                                c.Item().Text("RaceCoachFinder").Bold().FontSize(14);
                        });
                    });

                    col.Item().PaddingVertical(10).LineHorizontal(2).LineColor(Geel);

                    // ── Coach info + factuurdetails ──────────────────────
                    col.Item().Row(row =>
                    {
                        // Links: coach gegevens
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(coachNaam).Bold().FontSize(11);
                            if (!string.IsNullOrEmpty(coachAdres))
                                c.Item().Text(coachAdres).FontColor(Grijs);
                            if (!string.IsNullOrEmpty(coachPostcode) || !string.IsNullOrEmpty(coachStad))
                                c.Item().Text($"{coachPostcode} {coachStad}".Trim()).FontColor(Grijs);
                            if (!string.IsNullOrEmpty(coachLand))
                                c.Item().Text(coachLand).FontColor(Grijs);
                            if (!string.IsNullOrEmpty(coachTelefoon))
                                c.Item().Text(coachTelefoon).FontColor(Grijs);
                            c.Item().Text(coachEmail).FontColor(Grijs);
                            if (!string.IsNullOrEmpty(kvkNummer))
                                c.Item().PaddingTop(4).Text($"KVK: {kvkNummer}").FontColor(Grijs);
                            if (!string.IsNullOrEmpty(btwNummer))
                                c.Item().Text($"BTW: {btwNummer}").FontColor(Grijs);
                        });

                        // Rechts: factuurdetails
                        row.ConstantItem(200).Table(t =>
                        {
                            t.ColumnsDefinition(cd =>
                            {
                                cd.RelativeColumn();
                                cd.RelativeColumn();
                            });

                            void Rij(string label, string waarde, bool vet = false)
                            {
                                t.Cell().PaddingVertical(2).Text(label).FontColor(Grijs);
                                var cel = t.Cell().PaddingVertical(2).AlignRight().Text(waarde);
                                if (vet) cel.Bold();
                            }

                            Rij("Factuurnummer:", factuurnummer, true);
                            Rij("Datum:", datum.ToString("dd-MM-yyyy"));
                            Rij("Vervaldatum:", vervaldatum.ToString("dd-MM-yyyy"));
                        });
                    });

                    col.Item().PaddingTop(16).Text("Naar:").Bold().FontSize(9).FontColor(Grijs);
                    col.Item().Text(rijderNaam).Bold();
                    col.Item().Text(rijderEmail).FontColor(Grijs);

                    if (!string.IsNullOrEmpty(notities))
                    {
                        col.Item().PaddingTop(10).Text(notities).FontColor(Grijs).Italic();
                    }

                    col.Item().PaddingTop(16);

                    // ── Tabel ────────────────────────────────────────────
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(cd =>
                        {
                            cd.RelativeColumn(5);
                            cd.RelativeColumn(1.2f);
                            cd.RelativeColumn(2);
                            cd.RelativeColumn(1);
                            cd.RelativeColumn(2);
                            cd.RelativeColumn(2);
                        });

                        // Header
                        void Kop(string tekst, bool rechts = false)
                        {
                            var cell = t.Cell().Background(Zwart).Padding(6);
                            var txt = cell.Text(tekst).FontColor(Colors.White).Bold().FontSize(8);
                            if (rechts) txt.AlignRight();
                        }

                        Kop("Beschrijving");
                        Kop("Aantal", true);
                        Kop("Tarief", true);
                        Kop("BTW%", true);
                        Kop("BTW", true);
                        Kop("Totaal", true);

                        // Regels
                        bool donker = false;
                        foreach (var r in regels)
                        {
                            var sub = r.Aantal * r.Prijs;
                            var btwBedragRegel = sub * (r.Btw / 100.0);
                            var bg = donker ? LichtGrijs : "#FFFFFF";
                            donker = !donker;

                            void Cel(string tekst, bool rechts = false, bool vet = false)
                            {
                                var cell = t.Cell().Background(bg).Padding(5).BorderBottom(0.5f).BorderColor("#E0E0E0");
                                var txt = cell.Text(tekst);
                                if (rechts) txt.AlignRight();
                                if (vet) txt.Bold();
                            }

                            Cel(r.Omschrijving, vet: true);
                            Cel($"{r.Aantal:F2}".Replace(".", ","), rechts: true);
                            Cel(Eur(r.Prijs), rechts: true);
                            Cel($"{r.Btw:F0}%", rechts: true);
                            Cel(Eur(btwBedragRegel), rechts: true);
                            Cel(Eur(sub + btwBedragRegel), rechts: true);
                        }

                    });

                    // ── Totalen ──────────────────────────────────────────
                    col.Item().PaddingTop(8).AlignRight().Width(230).Table(t =>
                    {
                        t.ColumnsDefinition(cd =>
                        {
                            cd.RelativeColumn();
                            cd.ConstantColumn(100);
                        });

                        void TotaalRij(string label, string waarde, bool highlight = false)
                        {
                            var bg = highlight ? Geel : "#FFFFFF";
                            var fg = highlight ? Zwart : Grijs;
                            var l = t.Cell().Background(bg).PaddingVertical(4).PaddingLeft(8).Text(label).FontColor(fg);
                            var r = t.Cell().Background(bg).PaddingVertical(4).PaddingRight(8).AlignRight().Text(waarde).FontColor(fg);
                            if (highlight) { l.Bold(); r.Bold(); }
                        }

                        TotaalRij("Subtotaal excl. BTW", Eur(exclBtw));
                        TotaalRij("BTW", Eur(btwBedrag));
                        TotaalRij("TOTAALBEDRAG", Eur(totaal), highlight: true);
                    });
                });

                // Footer
                page.Footer().AlignCenter().PaddingTop(8)
                    .Text(t =>
                    {
                        t.Span("Betaling verloopt via ").FontColor(Grijs).FontSize(8);
                        t.Span("RaceCoachFinder").Bold().FontColor(Geel).FontSize(8);
                        t.Span(" — racecoachfinder.netlify.app").FontColor(Grijs).FontSize(8);
                    });
            });
        }).GeneratePdf();

        return (pdf, (decimal)Math.Round(totaal, 2));
    }

    public static FactuurData? ParseFactuurJson(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<FactuurData>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch { return null; }
    }
}
