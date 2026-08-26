using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchulungKK.Data;
using SchulungKK.Models;

namespace SchulungKK.Pages.Admin
{
    [RequestSizeLimit(1_073_741_824)]
    [RequestFormLimits(MultipartBodyLengthLimit = 1_073_741_824)]
    public class VideosModel : AdminPageModel
    {
        private const long MaximaleDateigroesse = 1_073_741_824;

        private static readonly string[] ErlaubteDateiendungen =
        {
            ".mp4",
            ".webm",
            ".ogg"
        };

        private readonly IWebHostEnvironment _environment;

        public VideosModel(SchulungenDbContext context, IWebHostEnvironment environment) : base(context)
        {
            _environment = environment;
        }

        public List<VideoAnzeige> Videos { get; set; } = new();

        [BindProperty]
        public IFormFile? UploadDatei { get; set; }

        [BindProperty]
        public int? BearbeitenId { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Bitte einen Titel eingeben.")]
        [StringLength(150, MinimumLength = 2, ErrorMessage = "Der Titel muss zwischen 2 und 150 Zeichen lang sein.")]
        public string Titel { get; set; } = string.Empty;

        [BindProperty]
        [StringLength(500, ErrorMessage = "Die Beschreibung darf höchstens 500 Zeichen lang sein.")]
        public string? Beschreibung { get; set; }

        [BindProperty]
        [Range(0, 10000, ErrorMessage = "Die Reihenfolge muss zwischen 0 und 10000 liegen.")]
        public int Reihenfolge { get; set; }

        public string BearbeitenDateiname { get; set; } = string.Empty;

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

            await LadeVideosAsync();

            if (!bearbeitenId.HasValue)
            {
                return Page();
            }

            Schulungsvideo? video = await Context.Schulungsvideos.AsNoTracking().FirstOrDefaultAsync(v => v.Id == bearbeitenId.Value);

            if (video == null)
            {
                Fehlermeldung = "Das ausgewählte Video wurde nicht gefunden.";

                return RedirectToPage();
            }

            BearbeitenId = video.Id;
            Titel = video.Titel;
            Beschreibung = video.Beschreibung;
            Reihenfolge = video.Reihenfolge;
            BearbeitenDateiname = video.Dateiname;

            return Page();
        }

        public async Task<IActionResult>
            OnPostHochladenAsync()
        {
            IActionResult? zugriffsErgebnis = await PruefeAdminZugriffAsync();

            if (zugriffsErgebnis != null)
            {
                return zugriffsErgebnis;
            }

            if (UploadDatei == null || UploadDatei.Length == 0)
            {
                Fehlermeldung = "Bitte wähle eine Videodatei aus.";

                return RedirectToPage();
            }

            if (UploadDatei.Length > MaximaleDateigroesse)
            {
                Fehlermeldung = "Die Videodatei ist größer als 1 GB.";

                return RedirectToPage();
            }

            string dateiendung = Path.GetExtension(UploadDatei.FileName).ToLowerInvariant();

            if (!ErlaubteDateiendungen.Contains(dateiendung, StringComparer.OrdinalIgnoreCase))
            {
                Fehlermeldung =
                    "Dieses Videoformat ist nicht erlaubt. " +
                    "Erlaubt sind MP4, WebM und OGG.";

                return RedirectToPage();
            }

            string dateiname = BereinigeDateiname(UploadDatei.FileName);

            string videoOrdner = ErmittleVideoOrdner();

            Directory.CreateDirectory(videoOrdner);

            string dateiPfad = Path.Combine(videoOrdner, dateiname);

            bool datenbankEintragExistiert = await Context.Schulungsvideos.AnyAsync(v => v.Dateiname == dateiname);

            if (datenbankEintragExistiert || System.IO.File.Exists(dateiPfad))
            {
                Fehlermeldung = $"Eine Datei mit dem Namen " + $"„{dateiname}“ existiert bereits.";

                return RedirectToPage();
            }

            int hoechsteReihenfolge = await Context.Schulungsvideos.Select(v => (int?)v.Reihenfolge).MaxAsync() ?? 0;

            var neuesVideo = new Schulungsvideo
                {
                    Titel = ErstelleTitelAusDateiname(dateiname),
                    Dateiname = dateiname,
                    Beschreibung = null,
                    Aktiv = true,
                    Reihenfolge = hoechsteReihenfolge + 10,
                    ErstelltAm = DateTime.Now
                };

            try
            {
                await using FileStream dateiStream = new FileStream(dateiPfad, FileMode.CreateNew, FileAccess.Write, FileShare.None);

                await UploadDatei.CopyToAsync(dateiStream);

                Context.Schulungsvideos.Add(neuesVideo);

                await Context.SaveChangesAsync();
            }
            catch (Exception exception)
                when (exception is IOException || exception is UnauthorizedAccessException || exception is DbUpdateException)
            {
                VersucheDateiZuLoeschen(dateiPfad);

                Fehlermeldung = "Das Video konnte nicht gespeichert werden. " + "Prüfe den Dateinamen, die Dateiberechtigungen " + "und die Datenbankverbindung.";

                return RedirectToPage();
            }

            Erfolgsmeldung = $"Das Video „{neuesVideo.Titel}“ " + "wurde erfolgreich hochgeladen.";

            return RedirectToPage();
        }

        public async Task<IActionResult>
            OnPostSpeichernAsync()
        {
            IActionResult? zugriffsErgebnis = await PruefeAdminZugriffAsync();

            if (zugriffsErgebnis != null)
            {
                return zugriffsErgebnis;
            }

            Titel = Titel?.Trim() ?? string.Empty;

            Beschreibung = string.IsNullOrWhiteSpace(Beschreibung) ? null : Beschreibung.Trim();

            if (!BearbeitenId.HasValue)
            {
                Fehlermeldung = "Es wurde kein Video zum Bearbeiten ausgewählt.";

                return RedirectToPage();
            }

            if (!ModelState.IsValid)
            {
                Schulungsvideo? aktuellesVideo = await Context.Schulungsvideos.AsNoTracking().FirstOrDefaultAsync(v => v.Id == BearbeitenId.Value);

                BearbeitenDateiname = aktuellesVideo?.Dateiname ?? string.Empty;

                await LadeVideosAsync();

                return Page();
            }

            Schulungsvideo? video = await Context.Schulungsvideos.FirstOrDefaultAsync(v => v.Id == BearbeitenId.Value);

            if (video == null)
            {
                Fehlermeldung = "Das Video wurde nicht gefunden.";

                return RedirectToPage();
            }

            video.Titel = Titel;
            video.Beschreibung = Beschreibung;
            video.Reihenfolge = Reihenfolge;

            await Context.SaveChangesAsync();

            Erfolgsmeldung = $"Das Video „{video.Titel}“ wurde aktualisiert.";

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

            Schulungsvideo? video = await Context.Schulungsvideos.FirstOrDefaultAsync(v => v.Id == id);

            if (video == null)
            {
                Fehlermeldung = "Das Video wurde nicht gefunden.";

                return RedirectToPage();
            }

            string titel = video.Titel;

            string sichererDateiname = Path.GetFileName(video.Dateiname);

            string dateiPfad = Path.Combine(ErmittleVideoOrdner(), sichererDateiname);

            try
            {
                Context.Schulungsvideos.Remove(video);

                /*
                 * Die Einträge in GruppeVideos werden
                 * durch die Cascade-Delete-Konfiguration
                 * automatisch entfernt.
                 */
                await Context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                Fehlermeldung = "Der Datenbankeintrag des Videos " + "konnte nicht gelöscht werden.";

                return RedirectToPage();
            }

            try
            {
                if (System.IO.File.Exists(dateiPfad))
                {
                    System.IO.File.Delete(dateiPfad);
                }
            }
            catch (UnauthorizedAccessException)
            {
                Fehlermeldung =
                    $"Das Video „{titel}“ wurde aus der " +
                    "Datenbank gelöscht. Die Videodatei konnte " +
                    "wegen fehlender Berechtigungen nicht gelöscht werden.";

                return RedirectToPage();
            }
            catch (IOException)
            {
                Fehlermeldung =
                    $"Das Video „{titel}“ wurde aus der " +
                    "Datenbank gelöscht. Die Videodatei konnte " +
                    "nicht gelöscht werden, weil sie verwendet wird.";

                return RedirectToPage();
            }

            Erfolgsmeldung = $"Das Video „{titel}“ wurde dauerhaft gelöscht.";

            return RedirectToPage();
        }

        private async Task LadeVideosAsync()
        {
            string videoOrdner = ErmittleVideoOrdner();

            Directory.CreateDirectory(videoOrdner);

            List<Schulungsvideo> datenbankVideos = await Context.Schulungsvideos.AsNoTracking().Include(v => v.GruppeVideos).OrderBy(v => v.Reihenfolge).ThenBy(v => v.Titel).ToListAsync();

            Videos = datenbankVideos.Select(video =>
                {
                    string dateiPfad = Path.Combine(videoOrdner, Path.GetFileName(video.Dateiname));

                    return new VideoAnzeige
                    {
                        Video = video,

                        DateiVorhanden = System.IO.File.Exists(dateiPfad),

                        VideoUrl = "/videos/" + Uri.EscapeDataString(video.Dateiname)
                    };
                }).ToList();
        }

        private string ErmittleVideoOrdner()
        {
            return Path.Combine(_environment.WebRootPath, "videos");
        }

        private static string BereinigeDateiname(string originalDateiname)
        {
            string sichererOriginalname = Path.GetFileName(originalDateiname);

            string dateiendung = Path.GetExtension(sichererOriginalname).ToLowerInvariant();

            string basisname = Path.GetFileNameWithoutExtension(sichererOriginalname);

            foreach (char ungueltigesZeichen in Path.GetInvalidFileNameChars())
            {
                basisname = basisname.Replace(ungueltigesZeichen, '_');
            }

            basisname = basisname.Trim().TrimEnd('.');

            if (string.IsNullOrWhiteSpace(basisname))
            {
                basisname = $"video-{DateTime.Now:yyyyMMdd-HHmmss}";
            }

            if (basisname.Length > 220)
            {
                basisname = basisname[..220];
            }

            return basisname + dateiendung;
        }

        private static string
            ErstelleTitelAusDateiname(string dateiname)
        {
            string titel = Path.GetFileNameWithoutExtension(dateiname).Replace('_', ' ').Replace('-', ' ').Trim();

            if (string.IsNullOrWhiteSpace(titel))
            {
                return "Schulungsvideo";
            }

            return titel.Length > 150 ? titel[..150] : titel;
        }

        private static void
            VersucheDateiZuLoeschen(string dateiPfad)
        {
            try
            {
                if (System.IO.File.Exists(dateiPfad))
                {
                    System.IO.File.Delete(dateiPfad);
                }
            }
            catch
            {
                /*
                 * Aufräumfehler nach einem fehlgeschlagenen
                 * Upload nicht erneut auslösen.
                 */
            }
        }

        public class VideoAnzeige
        {
            public Schulungsvideo Video { get; set; } = null!;

            public bool DateiVorhanden { get; set; }

            public string VideoUrl { get; set; } = string.Empty;
        }
    }
}