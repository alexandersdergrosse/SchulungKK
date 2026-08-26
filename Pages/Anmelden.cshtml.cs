using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchulungKK.Data;

namespace SchulungKK.Pages
{
    public class AnmeldenModel : PageModel
    {
        private readonly SchulungenDbContext _context;

        public AnmeldenModel(SchulungenDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string LoginName { get; set; } = string.Empty;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;

        public IActionResult OnGet()
        {
            if (HttpContext.Session.GetString("IsLoggedIn") == "true")
            {
                return RedirectToPage("/Index");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(LoginName) || string.IsNullOrEmpty(Password))
            {
                ErrorMessage = "Bitte Benutzername oder E-Mail und Passwort eingeben.";

                return Page();
            }

            string normalisierteEingabe = LoginName.Trim().ToLower();

            var benutzer = await _context.Benutzer.FirstOrDefaultAsync(b => b.Aktiv &&
                    (
                        b.Benutzername.ToLower() == normalisierteEingabe ||
                        b.Email.ToLower() == normalisierteEingabe
                    ));

            if (benutzer == null || benutzer.Passwort != Password)
            {
                ErrorMessage = "Benutzername, E-Mail oder Passwort ist falsch.";

                return Page();
            }

            HttpContext.Session.SetString("IsLoggedIn", "true");

            HttpContext.Session.SetString("Username", benutzer.Benutzername);

            HttpContext.Session.SetString("UserFullName", benutzer.Name);

            HttpContext.Session.SetString("UserId", benutzer.Id.ToString());

            HttpContext.Session.SetString("IsAdmin", benutzer.IstAdmin ? "true" : "false");

            benutzer.LetzterLogin = DateTime.Now;

            await _context.SaveChangesAsync();

            return RedirectToPage("/Index");
        }
    }
}