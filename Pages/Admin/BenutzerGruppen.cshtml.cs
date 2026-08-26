using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchulungKK.Data;
using SchulungKK.Models;

namespace SchulungKK.Pages.Admin
{
    public class BenutzerGruppenModel : AdminPageModel
    {
        public BenutzerGruppenModel(SchulungenDbContext context) : base(context)
        {
        }

        public List<Benutzer> BenutzerListe { get; set; } = new();

        public List<Gruppe> Gruppen { get; set; } = new();

        public Benutzer? AusgewaehlterBenutzer { get; set; }

        [BindProperty]
        public int BenutzerId { get; set; }

        [BindProperty]
        public List<int> AusgewaehlteGruppenIds { get; set; } = new();

        [TempData]
        public string? Erfolgsmeldung { get; set; }

        [TempData]
        public string? Fehlermeldung { get; set; }

        public async Task<IActionResult> OnGetAsync(int? benutzerId)
        {
            IActionResult? zugriffsErgebnis = await PruefeAdminZugriffAsync();

            if (zugriffsErgebnis != null)
            {
                return zugriffsErgebnis;
            }

            await LadeDatenAsync();

            if (benutzerId.HasValue)
            {
                AusgewaehlterBenutzer = BenutzerListe.FirstOrDefault(b => b.Id == benutzerId.Value);

                if (AusgewaehlterBenutzer == null)
                {
                    Fehlermeldung = "Der ausgewählte Benutzer wurde nicht gefunden.";

                    return RedirectToPage();
                }

                BenutzerId = AusgewaehlterBenutzer.Id;

                AusgewaehlteGruppenIds = AusgewaehlterBenutzer.BenutzerGruppen.Select(bg => bg.GruppeId).ToList();
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

            AusgewaehlteGruppenIds ??= new List<int>();

            List<int> neueGruppenIds = AusgewaehlteGruppenIds.Distinct().ToList();

            Benutzer? benutzer = await Context.Benutzer.Include(b => b.BenutzerGruppen).FirstOrDefaultAsync(b => b.Id == BenutzerId);

            if (benutzer == null)
            {
                Fehlermeldung = "Der Benutzer wurde nicht gefunden.";

                return RedirectToPage();
            }

            List<int> gueltigeGruppenIds = await Context.Gruppen.Where(g => neueGruppenIds.Contains(g.Id)).Select(g => g.Id).ToListAsync();

            if (gueltigeGruppenIds.Count != neueGruppenIds.Count)
            {
                Fehlermeldung = "Mindestens eine ausgewählte Gruppe existiert nicht mehr.";

                return RedirectToPage(new{benutzerId = BenutzerId});
            }

            HashSet<int> vorhandeneGruppenIds = benutzer.BenutzerGruppen.Select(bg => bg.GruppeId).ToHashSet();

            HashSet<int> gewuenschteGruppenIds = neueGruppenIds.ToHashSet();

            // Nicht mehr ausgewählte Zuordnungen entfernen.
            List<BenutzerGruppe> zuEntfernendeZuordnungen = benutzer.BenutzerGruppen.Where(bg => !gewuenschteGruppenIds.Contains(bg.GruppeId)).ToList();

            Context.BenutzerGruppen.RemoveRange(zuEntfernendeZuordnungen);

            // Neu ausgewählte Gruppen zuordnen.
            foreach (int gruppeId in gewuenschteGruppenIds)
            {
                if (vorhandeneGruppenIds.Contains(gruppeId))
                {
                    continue;
                }

                Context.BenutzerGruppen.Add(new BenutzerGruppe {BenutzerId = benutzer.Id, GruppeId = gruppeId, ZugeordnetAm = DateTime.Now});
            }

            await Context.SaveChangesAsync();

            Erfolgsmeldung = gewuenschteGruppenIds.Count == 0 ? $"Alle Gruppenzuordnungen von „{benutzer.Benutzername}“ wurden entfernt." : $"Die Gruppenzuordnungen von „{benutzer.Benutzername}“ wurden gespeichert.";



            return RedirectToPage(new{benutzerId = benutzer.Id});
        }

        private async Task LadeDatenAsync()
        {
            BenutzerListe = await Context.Benutzer.AsNoTracking().Include(b => b.BenutzerGruppen).ThenInclude(bg => bg.Gruppe).OrderByDescending(b => b.Aktiv).ThenBy(b => b.Benutzername).ToListAsync();

            Gruppen = await Context.Gruppen.AsNoTracking().OrderByDescending(g => g.Aktiv).ThenBy(g => g.Name).ToListAsync();
        }
    }
}