using Microsoft.EntityFrameworkCore;
using SchulungKK.Models;

namespace SchulungKK.Data
{
    public class SchulungenDbContext : DbContext
    {
        public SchulungenDbContext(DbContextOptions<SchulungenDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Benutzer> Benutzer => Set<Benutzer>();
        public virtual DbSet<QuizErgebnis> QuizErgebnisse => Set<QuizErgebnis>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Benutzer>(entity =>
            {
                entity.ToTable("Benutzer");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Benutzername).IsUnique();
                entity.Property(e => e.Benutzername).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Passwort).IsRequired().HasMaxLength(255);
                entity.Property(e => e.RegistriertAm).IsRequired();
                entity.Property(e => e.Aktiv).IsRequired();
            });

            modelBuilder.Entity<QuizErgebnis>(entity =>
            {
                entity.ToTable("QuizErgebnisse");
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Benutzer)
                      .WithMany(b => b.QuizErgebnisse)
                      .HasForeignKey(e => e.BenutzerId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
