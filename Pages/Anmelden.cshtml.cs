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
        public string Username { get; set; } = string.Empty;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;

        public void OnGet()
        {
            var isLoggedIn = HttpContext.Session.GetString("IsLoggedIn");
            if (!string.IsNullOrEmpty(isLoggedIn) && isLoggedIn == "true")
            {
                Response.Redirect("/Index");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    ErrorMessage = "Bitte Benutzername und Passwort eingeben!";
                    return Page();
                }

                var benutzer = await _context.Benutzer
                    .FirstOrDefaultAsync(b => b.Benutzername == Username && b.Aktiv == true);

                if (benutzer != null && benutzer.Passwort == Password)
                {
                    HttpContext.Session.SetString("IsLoggedIn", "true");
                    HttpContext.Session.SetString("Username", benutzer.Benutzername);
                    HttpContext.Session.SetString("UserFullName", benutzer.Name);
                    HttpContext.Session.SetString("UserId", benutzer.Id.ToString());

                    benutzer.LetzterLogin = DateTime.Now;
                    await _context.SaveChangesAsync();

                    return RedirectToPage("/Index");
                }

                ErrorMessage = "Benutzername oder Passwort falsch!";
                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Fehler: {ex.Message}";
                return Page();
            }
        }
    }
}
  