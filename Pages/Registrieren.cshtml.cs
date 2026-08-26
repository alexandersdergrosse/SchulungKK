using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchulungKK.Data;
using SchulungKK.Models;

namespace SchulungKK.Pages
{
    public class RegistrierenModel : PageModel
    {
        private readonly SchulungenDbContext _context;

        public RegistrierenModel(SchulungenDbContext context)
        {
            _context = context;
        }

        [BindProperty] public string Name { get; set; } = string.Empty;
        [BindProperty] public string Username { get; set; } = string.Empty;
        [BindProperty] public string Email { get; set; } = string.Empty;
        [BindProperty] public string Password { get; set; } = string.Empty;
        [BindProperty] public string PasswordConfirm { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;

        public void OnGet()
        {
            if (HttpContext.Session.GetString("IsLoggedIn") == "true")
            {
                Response.Redirect("/Index");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
                {
                    ErrorMessage = "Bitte alle Felder ausfüllen!";
                    return Page();
                }

                if (Username.Length < 3)
                {
                    ErrorMessage = "Benutzername muss mindestens 3 Zeichen lang sein!";
                    return Page();
                }

                if (Password.Length < 6)
                {
                    ErrorMessage = "Passwort muss mindestens 6 Zeichen lang sein!";
                    return Page();
                }

                if (Password != PasswordConfirm)
                {
                    ErrorMessage = "Passwörter stimmen nicht überein!";
                    return Page();
                }

                bool benutzernameVergeben = await _context.Benutzer.AnyAsync(b => b.Benutzername == Username);
                if (benutzernameVergeben)
                {
                    ErrorMessage = "Benutzername bereits vergeben!";
                    return Page();
                }

                bool emailVergeben = await _context.Benutzer.AnyAsync(b => b.Email == Email);
                if (emailVergeben)
                {
                    ErrorMessage = "E-Mail-Adresse bereits registriert!";
                    return Page();
                }

                // ? Passwort wird gehasht gespeichert
                var neuerBenutzer = new Benutzer
                {
                    Benutzername = Username,
                    Name = Name,
                    Email = Email,
                    Passwort = BCrypt.Net.BCrypt.HashPassword(Password),
                    RegistriertAm = DateTime.Now,
                    Aktiv = true
                };

                _context.Benutzer.Add(neuerBenutzer);
                await _context.SaveChangesAsync();

                SuccessMessage = "Registrierung erfolgreich! Sie können sich jetzt anmelden.";
                ViewData["RedirectToLogin"] = true;
                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Fehler beim Speichern: {ex.Message}";
                return Page();
            }
        }
    }
}
