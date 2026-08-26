using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchulungKK.Data;

namespace SchulungKK.Pages.Admin
{
    public class IndexModel : AdminPageModel
    {
        public IndexModel(SchulungenDbContext context) : base(context)
        {
        }

        public int BenutzerAnzahl { get; set; }

        public int GruppenAnzahl { get; set; }

        public int VideoAnzahl { get; set; }

        public int QuizAnzahl { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            IActionResult? zugriffsErgebnis = await PruefeAdminZugriffAsync();

            if (zugriffsErgebnis != null)
            {
                return zugriffsErgebnis;
            }

            BenutzerAnzahl = await Context.Benutzer.CountAsync();

            GruppenAnzahl = await Context.Gruppen.CountAsync();

            VideoAnzahl = await Context.Schulungsvideos.CountAsync();

            QuizAnzahl = await Context.VideoQuizze.CountAsync();

            return Page();
        }
    }
}