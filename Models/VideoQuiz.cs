namespace SchulungKK.Models
{
    public class VideoQuiz
    {
        public int Id { get; set; }

        public int SchulungsvideoId { get; set; }

        public string Titel { get; set; } = string.Empty;

        public string? Beschreibung { get; set; }

        public int Bestehensgrenze { get; set; } = 70;

        public int? MaximaleVersuche { get; set; }

        public int FragenAnzahl { get; set; }

        public string InhaltJson { get; set; } = string.Empty;

        public string Quelldateiname { get; set; } = string.Empty;

        public DateTime ErstelltAm { get; set; } = DateTime.Now;

        public DateTime AktualisiertAm { get; set; } = DateTime.Now;

        public virtual Schulungsvideo Schulungsvideo{ get; set; } = null!;

        public virtual ICollection<QuizErgebnis> QuizErgebnisse { get; set; } = new List<QuizErgebnis>();
    }
}