using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchulungKK.Data;
using SchulungKK.Models;

namespace SchulungKK.Pages.Admin
{
    public class GruppenModel : AdminPageModel
    {
        public GruppenModel(SchulungenDbContext context) : base(context)
        {
        }

        public List<Gruppe> Gruppen { get; set; } = new();

        [BindProperty]
        public int? BearbeitenId { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Bitte einen Gruppennamen eingeben.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Der Gruppenname muss zwischen 2 und 100 Zeichen lang sein.")]
        public string Name { get; set; } = string.Empty;

        [BindProperty]
        [StringLength(500, ErrorMessage = "Die Beschreibung darf höchstens 500 Zeichen lang sein.")]
        public string? Beschreibung { get; set; }

        [TempData]
        public string? Erfolgsmeldung { get; set; }

        [TempData]
        public string? Fehlermeldung { get; set; }

        public async Task<IActionResult> OnGetAsync(int? bearbeitenId)
        {
            IActionResult? zugriffsErgebnis = await PruefeAdminZugriffAsync();

            if (zugriffsErgebnis != null)
            {
                return zugriffsErgebnis;
            }

            await LadeGruppenAsync();

            if (bearbeitenId.HasValue)
            {
                Gruppe? gruppe = await Context.Gruppen.AsNoTracking().FirstOrDefaultAsync(g => g.Id == bearbeitenId.Value);

                if (gruppe == null)
                {
                    Fehlermeldung = "Die ausgewählte Gruppe wurde nicht gefunden.";

                    return RedirectToPage();
                }

                BearbeitenId = gruppe.Id;
                Name = gruppe.Name;
                Beschreibung = gruppe.Beschreibung;
            }

            return Page();
        }

        public async Task<IActionResult>
            OnPostSpeichernAsync()
        {
            IActionResult? zugriffsErgebnis = await PruefeAdminZugriffAsync();

            if (zugriffsErgebnis != null)
            {
                return zugriffsErgebnis;
            }

            Name = Name?.Trim() ?? string.Empty;
            Beschreibung = string.IsNullOrWhiteSpace(Beschreibung) ? null : Beschreibung.Trim();

            if (!ModelState.IsValid)
            {
                await LadeGruppenAsync();
                return Page();
            }

            string normalisierterName = Name.ToLower();

            bool nameBereitsVorhanden = await Context.Gruppen.AnyAsync(g => g.Id != BearbeitenId && g.Name.ToLower() == normalisierterName);

            if (nameBereitsVorhanden)
            {
                ModelState.AddModelError(nameof(Name), "Eine Gruppe mit diesem Namen existiert bereits.");

                await LadeGruppenAsync();
                return Page();
            }

            if (BearbeitenId.HasValue)
            {
                Gruppe? gruppe = await Context.Gruppen.FirstOrDefaultAsync(g => g.Id == BearbeitenId.Value);

                if (gruppe == null)
                {
                    Fehlermeldung = "Die Gruppe wurde nicht gefunden.";

                    return RedirectToPage();
                }

                gruppe.Name = Name;
                gruppe.Beschreibung = Beschreibung;

                Erfolgsmeldung = $"Die Gruppe „{Name}“ wurde aktualisiert.";
            }
            else
            {
                var neueGruppe = new Gruppe
                {
                    Name = Name,
                    Beschreibung = Beschreibung,
                    Aktiv = true,
                    ErstelltAm = DateTime.Now
                };

                Context.Gruppen.Add(neueGruppe);

                Erfolgsmeldung = $"Die Gruppe „{Name}“ wurde erstellt.";
            }

            await Context.SaveChangesAsync();

            return RedirectToPage();
        }

        public async Task<IActionResult>
            OnPostStatusAendernAsync(int id)
        {
            IActionResult? zugriffsErgebnis = await PruefeAdminZugriffAsync();

            if (zugriffsErgebnis != null)
            {
                return zugriffsErgebnis;
            }

            Gruppe? gruppe = await Context.Gruppen.FirstOrDefaultAsync(g => g.Id == id);

            if (gruppe == null)
            {
                Fehlermeldung = "Die Gruppe wurde nicht gefunden.";

                return RedirectToPage();
            }

            gruppe.Aktiv = !gruppe.Aktiv;

            await Context.SaveChangesAsync();

            Erfolgsmeldung = gruppe.Aktiv ? $"Die Gruppe „{gruppe.Name}“ wurde aktiviert." : $"Die Gruppe „{gruppe.Name}“ wurde deaktiviert.";

            return RedirectToPage();
        }

        public async Task<IActionResult>
            OnPostLoeschenAsync(int id)
                {
                    IActionResult? zugriffsErgebnis = await PruefeAdminZugriffAsync();

                    if (zugriffsErgebnis != null)
                    {
                        return zugriffsErgebnis;
                    }

                    Gruppe? gruppe = await Context.Gruppen.Include(g => g.BenutzerGruppen).Include(g => g.GruppeVideos).FirstOrDefaultAsync(g => g.Id == id);

                    if (gruppe == null)
                    {
                        Fehlermeldung = "Die Gruppe wurde nicht gefunden.";

                        return RedirectToPage();
                    }

                    string gruppenName = gruppe.Name;

                    int benutzerZuordnungen = gruppe.BenutzerGruppen.Count;

                    int videoZuordnungen = gruppe.GruppeVideos.Count;

                    Context.Gruppen.Remove(gruppe);

                    await Context.SaveChangesAsync();

                    Erfolgsmeldung = $"Die Gruppe „{gruppenName}“ wurde gelöscht. " + $"{benutzerZuordnungen} Benutzerzuordnung(en) und " + $"{videoZuordnungen} Videozuordnung(en) wurden entfernt.";

                    return RedirectToPage();
                }

        private async Task LadeGruppenAsync()
        {
            Gruppen = await Context.Gruppen.AsNoTracking().Include(g => g.BenutzerGruppen).Include(g => g.GruppeVideos).OrderByDescending(g => g.Aktiv).ThenBy(g => g.Name).ToListAsync();
        }
    }
}