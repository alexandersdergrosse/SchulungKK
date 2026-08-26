using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchulungKK.Data;

namespace SchulungKK.Pages.Admin
{
    public abstract class AdminPageModel : PageModel
    {
        protected readonly SchulungenDbContext Context;

        protected AdminPageModel(SchulungenDbContext context)
        {
            Context = context;
        }

        protected async Task<IActionResult?>
            PruefeAdminZugriffAsync()
        {
            string? isLoggedIn = HttpContext.Session.GetString("IsLoggedIn");

            string? userIdText = HttpContext.Session.GetString("UserId");

            if (isLoggedIn != "true" || !int.TryParse(userIdText, out int userId))
            {
                return RedirectToPage("/Anmelden");
            }

            bool istAdmin = await Context.Benutzer.AsNoTracking().AnyAsync(b => b.Id == userId && b.Aktiv && b.IstAdmin);

            if (!istAdmin)
            {
                HttpContext.Session.SetString("IsAdmin", "false");

                return RedirectToPage("/Error", new {statusCode = 403});
            }

            HttpContext.Session.SetString("IsAdmin", "true");

            return null;
        }
    }
}