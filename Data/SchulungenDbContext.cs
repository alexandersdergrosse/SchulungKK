using Microsoft.EntityFrameworkCore;
using SchulungKK.Models;

namespace SchulungKK.Data
{
    public class SchulungenDbContext : DbContext
    {
        public SchulungenDbContext(DbContextOptions<SchulungenDbContext> options) : base(options)
        {
        }

        public virtual DbSet<Benutzer> Benutzer => Set<Benutzer>();

        public virtual DbSet<QuizErgebnis> QuizErgebnisse => Set<QuizErgebnis>();

        public virtual DbSet<Gruppe> Gruppen => Set<Gruppe>();

        public virtual DbSet<BenutzerGruppe> BenutzerGruppen => Set<BenutzerGruppe>();

        public virtual DbSet<Schulungsvideo> Schulungsvideos => Set<Schulungsvideo>();

        public virtual DbSet<GruppeVideo> GruppeVideos => Set<GruppeVideo>();

        public virtual DbSet<VideoQuiz> VideoQuizze => Set<VideoQuiz>();

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

                entity.Property(e => e.IstAdmin).IsRequired();
            });

            modelBuilder.Entity<QuizErgebnis>(entity =>
            {
                entity.ToTable("QuizErgebnisse");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Benutzername).IsRequired().HasMaxLength(50);

                entity.Property(e => e.QuizName).IsRequired().HasMaxLength(100);

                entity.Property(e => e.Prozent).HasPrecision(5, 2);

                entity.HasOne(e => e.Benutzer).WithMany(b => b.QuizErgebnisse).HasForeignKey(e => e.BenutzerId).OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.VideoQuiz).WithMany(q => q.QuizErgebnisse).HasForeignKey(e => e.VideoQuizId).OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Gruppe>(entity =>
            {
                entity.ToTable("Gruppen");

                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.Name)
                      .IsUnique();

                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);

                entity.Property(e => e.Beschreibung).HasMaxLength(500);

                entity.Property(e => e.Aktiv).IsRequired();

                entity.Property(e => e.ErstelltAm).IsRequired();
            });

            modelBuilder.Entity<BenutzerGruppe>(entity =>
            {
                entity.ToTable("BenutzerGruppen");

                entity.HasKey(e => new
                {
                    e.BenutzerId,
                    e.GruppeId
                });

                entity.Property(e => e.ZugeordnetAm).IsRequired();

                entity.HasOne(e => e.Benutzer).WithMany(b => b.BenutzerGruppen).HasForeignKey(e => e.BenutzerId).OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Gruppe).WithMany(g => g.BenutzerGruppen).HasForeignKey(e => e.GruppeId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Schulungsvideo>(entity =>
            {
                entity.ToTable("Schulungsvideos");

                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.Dateiname).IsUnique();

                entity.Property(e => e.Titel).IsRequired().HasMaxLength(150);

                entity.Property(e => e.Dateiname).IsRequired().HasMaxLength(255);

                entity.Property(e => e.Beschreibung).HasMaxLength(500);

                entity.Property(e => e.Aktiv).IsRequired();

                entity.Property(e => e.Reihenfolge).IsRequired();

                entity.Property(e => e.ErstelltAm).IsRequired();
            });

            modelBuilder.Entity<GruppeVideo>(entity =>
            {
                entity.ToTable("GruppeVideos");

                entity.HasKey(e => new
                {
                    e.GruppeId,
                    e.SchulungsvideoId
                });

                entity.Property(e => e.ZugeordnetAm).IsRequired();

                entity.HasOne(e => e.Gruppe).WithMany(g => g.GruppeVideos).HasForeignKey(e => e.GruppeId).OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Schulungsvideo).WithMany(v => v.GruppeVideos).HasForeignKey(e => e.SchulungsvideoId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<VideoQuiz>(entity =>
            {
                entity.ToTable("VideoQuizze");

                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.SchulungsvideoId).IsUnique();

                entity.Property(e => e.Titel).IsRequired().HasMaxLength(150);

                entity.Property(e => e.Beschreibung).HasMaxLength(500);

                entity.Property(e => e.Bestehensgrenze).IsRequired();

                entity.Property(e => e.MaximaleVersuche);

                entity.Property(e => e.FragenAnzahl).IsRequired();

                entity.Property(e => e.InhaltJson).IsRequired().HasColumnType("nvarchar(max)");

                entity.Property(e => e.Quelldateiname).IsRequired().HasMaxLength(255);

                entity.Property(e => e.ErstelltAm).IsRequired();

                entity.Property(e => e.AktualisiertAm).IsRequired();

                entity.HasOne(e => e.Schulungsvideo).WithOne(v => v.Quiz).HasForeignKey<VideoQuiz>(e => e.SchulungsvideoId).OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}