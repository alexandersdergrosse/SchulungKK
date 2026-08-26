using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchulungKK.Data;
using SchulungKK.Models;
using System.ComponentModel.DataAnnotations;

namespace SchulungKK.Pages
{
    public class KontoModel : PageModel
    {
        private readonly SchulungenDbContext _context;

        public KontoModel(SchulungenDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string Benutzername { get; set; } = string.Empty;

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        public string AktuellesPasswort { get; set; } = string.Empty;

        [BindProperty]
        public string NeuesPasswort { get; set; } = string.Empty;

        [BindProperty]
        public string NeuesPasswortBestaetigung { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;

        [TempData]
        public string? SuccessMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!VersucheBenutzerIdZuLesen(out int benutzerId))
            {
                return RedirectToPage("/Anmelden");
            }

            Benutzer? benutzer = await _context.Benutzer.AsNoTracking().FirstOrDefaultAsync(b => b.Id == benutzerId && b.Aktiv);

            if (benutzer == null)
            {
                HttpContext.Session.Clear();
                return RedirectToPage("/Anmelden");
            }

            Benutzername = benutzer.Benutzername;
            Email = benutzer.Email;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!VersucheBenutzerIdZuLesen(out int benutzerId))
            {
                return RedirectToPage("/Anmelden");
            }

            Benutzer? benutzer = await _context.Benutzer.FirstOrDefaultAsync(b => b.Id == benutzerId && b.Aktiv);

            if (benutzer == null)
            {
                HttpContext.Session.Clear();
                return RedirectToPage("/Anmelden");
            }

            Benutzername = Benutzername.Trim();
            Email = Email.Trim();

            if (string.IsNullOrWhiteSpace(Benutzername) || string.IsNullOrWhiteSpace(Email) || string.IsNullOrEmpty(AktuellesPasswort))
            {
                ErrorMessage = "Bitte Benutzername, E-Mail und aktuelles Passwort eingeben.";

                return Page();
            }

            if (Benutzername.Length < 3)
            {
                ErrorMessage = "Der Benutzername muss mindestens 3 Zeichen lang sein.";

                return Page();
            }

            if (Benutzername.Length > 50)
            {
                ErrorMessage = "Der Benutzername darf höchstens 50 Zeichen lang sein.";

                return Page();
            }

            if (Email.Length > 100 || !new EmailAddressAttribute().IsValid(Email))
            {
                ErrorMessage = "Bitte eine gültige E-Mail-Adresse eingeben.";

                return Page();
            }

            if (benutzer.Passwort != AktuellesPasswort)
            {
                ErrorMessage = "Das aktuelle Passwort ist falsch.";

                return Page();
            }

            string normalisierterBenutzername = Benutzername.ToLower();

            bool benutzernameVergeben = await _context.Benutzer.AnyAsync(b => b.Id != benutzerId && b.Benutzername.ToLower() == normalisierterBenutzername);

            if (benutzernameVergeben)
            {
                ErrorMessage = "Dieser Benutzername ist bereits vergeben.";

                return Page();
            }

            string normalisierteEmail = Email.ToLower();

            bool emailVergeben = await _context.Benutzer.AnyAsync(b => b.Id != benutzerId && b.Email.ToLower() == normalisierteEmail);

            if (emailVergeben)
            {
                ErrorMessage = "Diese E-Mail-Adresse wird bereits verwendet.";

                return Page();
            }

            bool passwortSollGeaendertWerden = !string.IsNullOrEmpty(NeuesPasswort) || !string.IsNullOrEmpty(NeuesPasswortBestaetigung);

            if (passwortSollGeaendertWerden)
            {
                if (NeuesPasswort.Length < 6)
                {
                    ErrorMessage = "Das neue Passwort muss mindestens 6 Zeichen lang sein.";

                    return Page();
                }

                if (NeuesPasswort != NeuesPasswortBestaetigung)
                {
                    ErrorMessage = "Die neuen Passwörter stimmen nicht überein.";

                    return Page();
                }
            }

            string alterBenutzername = benutzer.Benutzername;

            benutzer.Benutzername = Benutzername;
            benutzer.Email = Email;

            if (passwortSollGeaendertWerden)
            {
                benutzer.Passwort = NeuesPasswort;
            }

            // Bereits gespeicherte Quiz-Ergebnisse ebenfalls
            // auf den neuen Benutzernamen aktualisieren.
            if (!string.Equals(alterBenutzername, Benutzername, StringComparison.Ordinal))
            {
                List<QuizErgebnis> ergebnisse = await _context.QuizErgebnisse.Where(e => e.BenutzerId == benutzerId).ToListAsync();

                foreach (QuizErgebnis ergebnis in ergebnisse)
                {
                    ergebnis.Benutzername = Benutzername;
                }
            }

            await _context.SaveChangesAsync();

            // Das Dropdown-Menü verwendet den Namen aus der Session.
            HttpContext.Session.SetString("Username", Benutzername);

            SuccessMessage = "Deine Kontodaten wurden erfolgreich geändert.";

            return RedirectToPage();
        }

        private bool VersucheBenutzerIdZuLesen(out int benutzerId)
        {
            benutzerId = 0;

            string? isLoggedIn = HttpContext.Session.GetString("IsLoggedIn");

            string? benutzerIdText = HttpContext.Session.GetString("UserId");

            return isLoggedIn == "true" && int.TryParse(benutzerIdText, out benutzerId);
        }
    }
}