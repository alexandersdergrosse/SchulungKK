using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SchulungKK.Pages
{
    public class AbmeldenModel : PageModel
    {
        public IActionResult OnGet()
        {
            // Session löschen
            HttpContext.Session.Clear();

            // Zur Anmelde-Seite umleiten
            return RedirectToPage("/Anmelden");
        }

        public IActionResult OnPost()
        {
            // Session löschen
            HttpContext.Session.Clear();

            // Zur Anmelde-Seite umleiten
            return RedirectToPage("/Anmelden");
        }
    }
}