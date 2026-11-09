using Microsoft.AspNetCore.Authentication.JwtBearer;
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

// Admin-account: het wachtwoord komt uit de omgevingsvariabele ADMIN_WACHTWOORD (in Railway instellen).
// Het oude standaardwachtwoord staat in deze openbare code en wordt daarom bij inloggen geweigerd (zie AuthController).
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var adminEmail = "admin@mail";
    var adminWachtwoord = Environment.GetEnvironmentVariable("ADMIN_WACHTWOORD");
    var admin = await userManager.FindByEmailAsync(adminEmail);
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

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
