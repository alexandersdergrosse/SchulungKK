using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SchulungKK.Pages
{
    public class IndexModel : PageModel
    {
        public string CurrentUsername { get; set; } = string.Empty;

        public IActionResult OnGet()
        {
            var isLoggedIn = HttpContext.Session.GetString("IsLoggedIn");

            if (string.IsNullOrEmpty(isLoggedIn) || isLoggedIn != "true")
            {
                return RedirectToPage("/Anmelden");
            }

            CurrentUsername = HttpContext.Session.GetString("Username") ?? "Unbekannt";
            ViewData["Username"] = CurrentUsername;

            return Page();
        }
    }
}
