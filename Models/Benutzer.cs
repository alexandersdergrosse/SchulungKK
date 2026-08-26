
namespace SchulungKK.Models
{
    public class Benutzer
    {
        public int Id { get; set; }

        public string Benutzername { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Passwort { get; set; } = string.Empty;

        public DateTime RegistriertAm { get; set; } = DateTime.Now;

        public DateTime? LetzterLogin { get; set; }

        public bool Aktiv { get; set; } = true;

        // Bestimmt, ob der Benutzer Verwaltungsrechte besitzt.
        public bool IstAdmin { get; set; } = false;

        public virtual ICollection<QuizErgebnis> QuizErgebnisse { get; set; } = new List<QuizErgebnis>();

        public virtual ICollection<BenutzerGruppe> BenutzerGruppen { get; set; } = new List<BenutzerGruppe>();
    }
}