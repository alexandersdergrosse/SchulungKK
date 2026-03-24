using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        public virtual ICollection<QuizErgebnis> QuizErgebnisse { get; set; } = new List<QuizErgebnis>();
    }
}