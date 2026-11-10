using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Backend.Data;
using Backend.Models;
using Backend.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var dbPath = Environment.GetEnvironmentVariable("DB_PATH") ?? "coachfinder.db";
var dbDir = Path.GetDirectoryName(dbPath);
if (!string.IsNullOrEmpty(dbDir)) Directory.CreateDirectory(dbDir);
// Pushmeldingen: elk nieuw (groeps)bericht gaat via de wachtrij naar de telefoons van de ontvangers
builder.Services.AddSingleton<PushWachtrij>();
builder.Services.AddSingleton<PushMeldingInterceptor>();
builder.Services.AddScoped<PushService>();
builder.Services.AddHostedService<PushVerzender>();
builder.Services.AddSingleton<GratisMeldingWachtrij>();
builder.Services.AddHostedService<GratisMeldingVerzender>();
builder.Services.AddScoped<AccountVerwijderService>();
builder.Services.AddScoped<FactuurService>();
builder.Services.AddScoped<WachtlijstMelder>();
builder.Services.AddScoped<MollieConnectService>();

// Versleuteling (o.a. Mollie-tokens van coaches). De sleutels staan naast de database, zodat ze een
// nieuwe deploy overleven.
builder.Services.AddDataProtection()
    .SetApplicationName("RaceCoachFinder")
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(string.IsNullOrEmpty(dbDir) ? Directory.GetCurrentDirectory() : dbDir, "sleutels")));

// Mollie Connect. Alleen lokaal (Development) kan al het Mollie-verkeer naar een nep-server.
var mollieNep = builder.Environment.IsDevelopment() ? Environment.GetEnvironmentVariable("MOLLIE_NEP_URL") : null;
var mollieHttp = builder.Services.AddHttpClient(MollieConnectService.HttpNaam);
if (!string.IsNullOrEmpty(mollieNep))
{
    MollieConnectService.NepUrl = mollieNep.TrimEnd('/');
    mollieHttp.ConfigurePrimaryHttpMessageHandler(() => new MollieOmleiding(mollieNep));
}
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
    options.UseSqlite($"Data Source={dbPath}")
           .AddInterceptors(sp.GetRequiredService<PushMeldingInterceptor>())
           .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8; // geldt voor nieuwe wachtwoorden; bestaande blijven werken
    options.User.RequireUniqueEmail = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.AllowedForNewUsers = true;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

var jwtSleutel = builder.Configuration["Jwt:Sleutel"]!;
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Uitgever"],
        ValidAudience = builder.Configuration["Jwt:Publiek"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSleutel))
    };
});

builder.Services.AddSingleton<IEmailService, EmailService>();
builder.Services.AddSingleton<IWachtwoordResetService, WachtwoordResetService>();
builder.Services.AddSingleton<IEmailVerificatieService, EmailVerificatieService>();
builder.Services.AddHostedService<AgendaReminderService>();
// Spambeveiliging: maximaal aantal verzoeken per IP-adres (achter de proxy van Railway via X-Forwarded-For)
static string ClientIp(HttpContext c) =>
    (c.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim() is { Length: > 0 } ip ? ip : null)
    ?? c.Connection.RemoteIpAddress?.ToString() ?? "onbekend";
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
    o.OnRejected = async (ctx, ct) =>
    {
        ctx.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
        await ctx.HttpContext.Response.WriteAsync("Je hebt dit te vaak gedaan. Wacht even en probeer het later opnieuw.", ct);
    };
    // Contactformulier en coach rapporteren: 3 per 10 minuten
    o.AddPolicy("formulier", c => System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(ClientIp(c),
        _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions { PermitLimit = 3, Window = TimeSpan.FromMinutes(10) }));
    // Endpoints die mails versturen (registreren, wachtwoord vergeten, code opnieuw): 6 per 15 minuten
    o.AddPolicy("mail", c => System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(ClientIp(c),
        _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions { PermitLimit = 6, Window = TimeSpan.FromMinutes(15) }));
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

// Automatisch migraties toepassen bij opstarten
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Admin-account: racecoachfinder@gmail.com (was admin@mail). Het wachtwoord komt uit de omgevingsvariabele
// ADMIN_WACHTWOORD (in Railway instellen). Het oude standaardwachtwoord stond in deze openbare code en wordt
// daarom bij inloggen geweigerd (zie AuthController).
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    const string adminEmail = "racecoachfinder@gmail.com";
    const string oudAdminEmail = "admin@mail";
    var adminWachtwoord = Environment.GetEnvironmentVariable("ADMIN_WACHTWOORD");
    static bool IsAdmin(ApplicationUser u) => (u.Rol ?? "").Split(',').Any(r => r.Trim() == "Admin");

    var admin = await userManager.FindByEmailAsync(adminEmail);
    if (admin != null && !IsAdmin(admin))
    {
        app.Logger.LogWarning("{Email} hoort bij een gewoon account; het admin-account blijft {Oud}.", adminEmail, oudAdminEmail);
        admin = await userManager.FindByEmailAsync(oudAdminEmail);
    }
    else if (admin == null)
    {
        // Bestaand admin-account overzetten naar het nieuwe e-mailadres
        admin = await userManager.FindByEmailAsync(oudAdminEmail);
        if (admin != null)
        {
            admin.Email = adminEmail;
            admin.UserName = adminEmail;
            admin.NormalizedEmail = adminEmail.ToUpperInvariant();
            admin.NormalizedUserName = adminEmail.ToUpperInvariant();
            admin.EmailConfirmed = true;
            var r = await userManager.UpdateAsync(admin);
            if (!r.Succeeded) app.Logger.LogWarning("Admin-e-mail wijzigen mislukt: {Fout}", string.Join(", ", r.Errors.Select(e => e.Description)));
        }
    }

    if (admin == null && !string.IsNullOrWhiteSpace(adminWachtwoord))
    {
        admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            Naam = "Administrator",
            Rol = "Admin",
            EmailConfirmed = true
        };
        await userManager.CreateAsync(admin, adminWachtwoord);
    }
    else if (admin != null && !string.IsNullOrWhiteSpace(adminWachtwoord) && !await userManager.CheckPasswordAsync(admin, adminWachtwoord))
    {
        var resetToken = await userManager.GeneratePasswordResetTokenAsync(admin);
        var resultaat = await userManager.ResetPasswordAsync(admin, resetToken, adminWachtwoord);
        if (!resultaat.Succeeded)
            app.Logger.LogWarning("ADMIN_WACHTWOORD kon niet worden ingesteld: {Fout}", string.Join(", ", resultaat.Errors.Select(e => e.Description)));
    }
}

// API-documentatie alleen tijdens het ontwikkelen, niet openbaar online
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors();
app.UseRateLimiter();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
