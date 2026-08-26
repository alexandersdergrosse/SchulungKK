namespace SchulungKK.Models
{
    public class Gruppe
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Beschreibung { get; set; }

        public bool Aktiv { get; set; } = true;

        public DateTime ErstelltAm { get; set; } = DateTime.Now;

        public virtual ICollection<BenutzerGruppe> BenutzerGruppen { get; set; } = new List<BenutzerGruppe>();

        public virtual ICollection<GruppeVideo> GruppeVideos { get; set; } = new List<GruppeVideo>();
    }
}