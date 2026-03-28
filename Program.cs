using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ThesisWebApp.Data;
using ThesisWebApp.Models;
using ThesisWebApp.Services;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection(EmailSettings.SectionName));
builder.Services.AddTransient<IEmailService, EmailService>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDbContext<ApplicationIdentityDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationIdentityDbContext>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(defaultConnection))
{
    app.Logger.LogError(
        "DefaultConnection is null or empty — skipping EF Core migrations and identity seed. " +
        "Configure a valid PostgreSQL connection string (e.g. on Render set environment variable ConnectionStrings__DefaultConnection). " +
        "Expected format: Host=...;Port=5432;Database=...;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true");
}
else
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var config = services.GetRequiredService<IConfiguration>();
        var connectionString = config.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            Console.WriteLine("@@@ GREŠKA: Connection String nije pronađen u konfiguraciji! @@@");
        }
        else
        {
            Console.WriteLine("@@@ Pokušavam migraciju baze podataka... @@@");
            await services.GetRequiredService<ApplicationIdentityDbContext>().Database.MigrateAsync();
            await services.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
            await IdentitySeeder.SeedAsync(services);
            Console.WriteLine("@@@ Migracija i Seed završeni uspešno! @@@");
        }
    }
}

app.Run();
