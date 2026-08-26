namespace SchulungKK.Models
{
    public class GruppeVideo
    {
        public int GruppeId { get; set; }

        public int SchulungsvideoId { get; set; }

        public DateTime ZugeordnetAm { get; set; } = DateTime.Now;

        public virtual Gruppe Gruppe { get; set; } = null!;

        public virtual Schulungsvideo Schulungsvideo { get; set; } = null!;
    }
}