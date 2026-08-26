namespace SchulungKK.Models
{
    public class Schulungsvideo
    {
        public int Id { get; set; }

        public string Titel { get; set; } = string.Empty;

        public string Dateiname { get; set; } = string.Empty;

        public string? Beschreibung { get; set; }

        public bool Aktiv { get; set; } = true;

        public int Reihenfolge { get; set; }

        public DateTime ErstelltAm { get; set; } = DateTime.Now;

        public virtual ICollection<GruppeVideo> GruppeVideos { get; set; } = new List<GruppeVideo>();

        public virtual VideoQuiz? Quiz { get; set; }
        }
}