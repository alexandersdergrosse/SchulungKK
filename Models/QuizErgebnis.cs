using System;
using System.ComponentModel.DataAnnotations;

namespace SchulungKK.Models
{
    public class QuizErgebnis
    {
        public int Id { get; set; }
        public int BenutzerId { get; set; }
        public string QuizName { get; set; } = string.Empty;
        public int Richtig { get; set; }
        public int Gesamt { get; set; }
        public decimal Prozent { get; set; }
        public bool Bestanden { get; set; }
        public DateTime AbgeschlossenAm { get; set; } = DateTime.Now;
        public virtual Benutzer? Benutzer { get; set; }
    }
}
