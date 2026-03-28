using Microsoft.EntityFrameworkCore;
using ThesisWebApp.Models;

namespace ThesisWebApp.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Most> Mostovi { get; set; } = null!;
    public DbSet<LokacijaPredef> LokacijePredef { get; set; } = null!;
    public DbSet<Merenje> Merenja { get; set; } = null!;
    public DbSet<Postavke> Postavke { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Primarni ključevi
        modelBuilder.Entity<LokacijaPredef>()
            .HasKey(l => l.Ime);

        modelBuilder.Entity<Most>()
            .HasKey(m => m.Id);

        modelBuilder.Entity<LokacijaPredef>()
            .HasOne(l => l.Most)
            .WithMany(m => m.Lokacije)
            .HasForeignKey(l => l.MostId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Postavke>()
            .HasKey(p => p.Kljuc);

        modelBuilder.Entity<Merenje>()
            .HasKey(m => new { m.Ime, m.Dt });

        // Veza preko kolone "ime"
        modelBuilder.Entity<Merenje>()
            .HasOne(m => m.Lokacija)
            .WithMany(l => l.Merenja)
            .HasForeignKey(m => m.Ime)
            .HasPrincipalKey(l => l.Ime)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

