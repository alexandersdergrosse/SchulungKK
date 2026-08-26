using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchulungKK.Data;
using SchulungKK.Models;

namespace SchulungKK.Pages.Admin
{
    public class VideoGruppenModel : AdminPageModel
    {
        public VideoGruppenModel(SchulungenDbContext context) : base(context)
        {
        }

        public List<Schulungsvideo> VideoListe { get; set; } = new();

        public List<Gruppe> Gruppen { get; set; } = new();

        public Schulungsvideo? AusgewaehltesVideo { get; set; }

        [BindProperty]
        public int VideoId { get; set; }

        [BindProperty]
        public List<int> AusgewaehlteGruppenIds { get; set; } = new();

        [TempData]
        public string? Erfolgsmeldung { get; set; }

        [TempData]
        public string? Fehlermeldung { get; set; }

        public async Task<IActionResult> OnGetAsync(int? videoId)
        {
            IActionResult? zugriffsErgebnis = await PruefeAdminZugriffAsync();

            if (zugriffsErgebnis != null)
            {
                return zugriffsErgebnis;
            }

            await LadeDatenAsync();

            if (videoId.HasValue)
            {
                AusgewaehltesVideo = VideoListe.FirstOrDefault(v => v.Id == videoId.Value);

                if (AusgewaehltesVideo == null)
                {
                    Fehlermeldung = "Das ausgewählte Video wurde nicht gefunden.";

                    return RedirectToPage();
                }

                VideoId = AusgewaehltesVideo.Id;

                AusgewaehlteGruppenIds = AusgewaehltesVideo.GruppeVideos.Select(gv => gv.GruppeId).ToList();
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

            Schulungsvideo? video = await Context.Schulungsvideos.Include(v => v.GruppeVideos).FirstOrDefaultAsync(v => v.Id == VideoId);

            if (video == null)
            {
                Fehlermeldung = "Das Video wurde nicht gefunden.";

                return RedirectToPage();
            }

            List<int> gueltigeGruppenIds = await Context.Gruppen.Where(g => neueGruppenIds.Contains(g.Id)).Select(g => g.Id).ToListAsync();

            if (gueltigeGruppenIds.Count != neueGruppenIds.Count)
            {
                Fehlermeldung = "Mindestens eine ausgewählte Gruppe existiert nicht mehr.";

                return RedirectToPage(
                    new
                    {
                        videoId = VideoId
                    });
            }

            HashSet<int> vorhandeneGruppenIds = video.GruppeVideos.Select(gv => gv.GruppeId).ToHashSet();

            HashSet<int> gewuenschteGruppenIds = neueGruppenIds.ToHashSet();

            // Nicht mehr ausgewählte Zuordnungen entfernen.
            List<GruppeVideo> zuEntfernendeZuordnungen = video.GruppeVideos.Where(gv => !gewuenschteGruppenIds.Contains(gv.GruppeId)).ToList();

            Context.GruppeVideos.RemoveRange(zuEntfernendeZuordnungen);

            // Neu ausgewählte Gruppen zuordnen.
            foreach (int gruppeId in gewuenschteGruppenIds)
            {
                if (vorhandeneGruppenIds.Contains(gruppeId))
                {
                    continue;
                }

                Context.GruppeVideos.Add(new GruppeVideo
                    {
                        GruppeId = gruppeId,
                        SchulungsvideoId = video.Id,
                        ZugeordnetAm = DateTime.Now
                    });
            }

            await Context.SaveChangesAsync();

            Erfolgsmeldung = gewuenschteGruppenIds.Count == 0 ? $"Alle Gruppenzuordnungen von „{video.Titel}“ wurden entfernt." : $"Die Gruppenzuordnungen von „{video.Titel}“ wurden gespeichert.";

            return RedirectToPage(
                new
                {
                    videoId = video.Id
                });
        }

        private async Task LadeDatenAsync()
        {
            VideoListe = await Context.Schulungsvideos
                .AsNoTracking()
                .Include(v => v.GruppeVideos)
                .ThenInclude(gv => gv.Gruppe)
                .OrderByDescending(v => v.Aktiv)
                .ThenBy(v => v.Reihenfolge)
                .ThenBy(v => v.Titel)
                .ToListAsync();

            Gruppen = await Context.Gruppen
                .AsNoTracking()
                .OrderByDescending(g => g.Aktiv)
                .ThenBy(g => g.Name)
                .ToListAsync();
        }
    }
}