namespace SchulungKK.Models
{
    public class BenutzerGruppe
    {
        public int BenutzerId { get; set; }

        public int GruppeId { get; set; }

        public DateTime ZugeordnetAm { get; set; } = DateTime.Now;

        public virtual Benutzer Benutzer { get; set; } = null!;

        public virtual Gruppe Gruppe { get; set; } = null!;
    }
}