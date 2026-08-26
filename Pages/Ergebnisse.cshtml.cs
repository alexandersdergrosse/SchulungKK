using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchulungKK.Data;
using SchulungKK.Models;
using SchulungKK.Services;

namespace SchulungKK.Pages
{
    public class ErgebnisseModel : PageModel
    {
        private readonly SchulungenDbContext _context;
        private readonly ZertifikatService _zertifikatService;

        public ErgebnisseModel(SchulungenDbContext context, ZertifikatService zertifikatService)
        {
            _context = context;
            _zertifikatService = zertifikatService;
        }

        public List<QuizErgebnis> Ergebnisse { get; set; } = new();

        public string Benutzername { get; set; } = string.Empty;

        [TempData]
        public string? Fehlermeldung { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!VersucheBenutzerIdZuLesen(out int benutzerId))
            {
                return RedirectToPage("/Anmelden");
            }

            Benutzername = HttpContext.Session.GetString("Username") ?? "Unbekannt";

            Ergebnisse = await _context.QuizErgebnisse.AsNoTracking().Where(e => e.BenutzerId == benutzerId).OrderByDescending(e => e.AbgeschlossenAm).ToListAsync();

            return Page();
        }

        public async Task<IActionResult>
            OnGetZertifikatAsync(int id)
        {
            if (!VersucheBenutzerIdZuLesen(out int benutzerId))
            {
                return RedirectToPage("/Anmelden");
            }

            QuizErgebnis? ergebnis = await _context.QuizErgebnisse.AsNoTracking().Include(e => e.Benutzer).FirstOrDefaultAsync(e => e.Id == id && e.BenutzerId == benutzerId);

            if (ergebnis == null)
            {
                return NotFound();
            }

            if (!ergebnis.Bestanden)
            {
                Fehlermeldung = "Ein Zertifikat kann nur für eine bestandene Schulung erstellt werden.";

                return RedirectToPage();
            }

            byte[] pdfDatei = _zertifikatService.ErstelleZertifikat(ergebnis);

            string teilnehmerName = !string.IsNullOrWhiteSpace(ergebnis.Benutzer?.Name) ? ergebnis.Benutzer.Name : ergebnis.Benutzername;

            string sichererName = BereinigeDateiname(teilnehmerName);

            string dateiname = $"Zertifikat_{sichererName}_{ergebnis.Id:D6}.pdf";

            return File(pdfDatei, "application/pdf", dateiname);
        }

        private bool VersucheBenutzerIdZuLesen(out int benutzerId)
        {
            benutzerId = 0;

            string? istAngemeldet = HttpContext.Session.GetString("IsLoggedIn");

            string? benutzerIdText = HttpContext.Session.GetString("UserId");

            return istAngemeldet == "true" && int.TryParse(benutzerIdText, out benutzerId);
        }

        private static string BereinigeDateiname(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "Teilnehmer";
            }

            string bereinigterName = name.Trim().Replace(' ', '_');

            foreach (char ungueltigesZeichen in Path.GetInvalidFileNameChars())
            {
                bereinigterName = bereinigterName.Replace(ungueltigesZeichen, '_');
            }

            return bereinigterName;
        }
    }
}