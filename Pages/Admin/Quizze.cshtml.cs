using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchulungKK.Data;
using SchulungKK.Models;
using SchulungKK.Services;

namespace SchulungKK.Pages.Admin
{
    [RequestSizeLimit(6_291_456)]
    [RequestFormLimits(MultipartBodyLengthLimit = 6_291_456)]
    public class QuizzeModel : AdminPageModel
    {
        private const long MaximaleDateigroesse = 5_242_880;

        private static readonly JsonSerializerOptions JsonOptionen = new(JsonSerializerDefaults.Web);

        private readonly WordQuizImportService _wordQuizImportService;

        public QuizzeModel(SchulungenDbContext context, WordQuizImportService wordQuizImportService) : base(context)
        {
            _wordQuizImportService = wordQuizImportService;
        }

        public List<Schulungsvideo> Videos { get; set; } = new();

        [BindProperty]
        public int VideoId { get; set; }

        [BindProperty]
        public IFormFile? WordDatei { get; set; }

        [BindProperty]
        public int? MaximaleVersuche { get; set; } = 3;

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

            await LadeVideosAsync();

            MaximaleVersuche = 3;

            if (videoId.HasValue)
            {
                Schulungsvideo? ausgewaehltesVideo = Videos.FirstOrDefault(video => video.Id == videoId.Value);

                if (ausgewaehltesVideo != null)
                {
                    VideoId = ausgewaehltesVideo.Id;

                    if (ausgewaehltesVideo.Quiz != null)
                    {
                        MaximaleVersuche = ausgewaehltesVideo.Quiz.MaximaleVersuche;
                    }
                }
            }

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

            if (MaximaleVersuche.HasValue && (MaximaleVersuche.Value < 1 || MaximaleVersuche.Value > 20))
            {
                Fehlermeldung = "Die Anzahl der Wiederholungen muss zwischen 0 und 20 liegen oder leer bleiben.";

                return RedirectToPage(new { videoId = VideoId });
            }

            Schulungsvideo? video = await Context.Schulungsvideos.Include(v => v.Quiz).FirstOrDefaultAsync(v => v.Id == VideoId);

            if (video == null)
            {
                Fehlermeldung = "Bitte wähle ein gültiges Video aus.";

                return RedirectToPage();
            }

            if (WordDatei == null || WordDatei.Length == 0)
            {
                Fehlermeldung = "Bitte wähle eine Word-Datei aus.";

                return RedirectToPage(
                    new
                    {
                        videoId = VideoId
                    });
            }

            if (WordDatei.Length > MaximaleDateigroesse)
            {
                Fehlermeldung = "Die Word-Datei darf höchstens 5 MB groß sein.";

                return RedirectToPage(
                    new
                    {
                        videoId = VideoId
                    });
            }

            string dateiendung = Path.GetExtension(WordDatei.FileName);

            if (!string.Equals(dateiendung, ".docx", StringComparison.OrdinalIgnoreCase))
            {
                Fehlermeldung = "Es sind ausschließlich DOCX-Dateien erlaubt.";

                return RedirectToPage(
                    new
                    {
                        videoId = VideoId
                    });
            }

            WordQuizImportErgebnis importErgebnis;

            try
            {
                importErgebnis = await _wordQuizImportService.ImportiereAsync(WordDatei);
            }
            catch (QuizImportException exception)
            {
                Fehlermeldung = exception.Message;

                return RedirectToPage(
                    new
                    {
                        videoId = VideoId
                    });
            }

            string inhaltJson = JsonSerializer.Serialize(importErgebnis.QuizDaten, JsonOptionen);

            bool wirdErsetzt = video.Quiz != null;

            if (video.Quiz == null)
            {
                video.Quiz = new VideoQuiz
                    {
                        SchulungsvideoId = video.Id,
                        ErstelltAm = DateTime.Now
                    };
            }

            video.Quiz.Titel = importErgebnis.Titel;

            video.Quiz.Beschreibung = importErgebnis.Beschreibung;

            video.Quiz.Bestehensgrenze = importErgebnis.Bestehensgrenze;

            video.Quiz.MaximaleVersuche = MaximaleVersuche;

            video.Quiz.FragenAnzahl = importErgebnis.QuizDaten.Fragen.Count;

            video.Quiz.InhaltJson =inhaltJson;

            video.Quiz.Quelldateiname = Path.GetFileName(WordDatei.FileName);

            video.Quiz.AktualisiertAm = DateTime.Now;

            await Context.SaveChangesAsync();

            Erfolgsmeldung = wirdErsetzt ? $"Das Quiz für „{video.Titel}“ wurde ersetzt." : $"Das Quiz wurde mit „{video.Titel}“ verbunden.";

            return RedirectToPage(
                new
                {
                    videoId = video.Id
                });
        }

        public async Task<IActionResult>
            OnPostLoeschenAsync(int id)
        {
            IActionResult? zugriffsErgebnis = await PruefeAdminZugriffAsync();

            if (zugriffsErgebnis != null)
            {
                return zugriffsErgebnis;
            }

            VideoQuiz? quiz = await Context.VideoQuizze.Include(q => q.Schulungsvideo).FirstOrDefaultAsync(q => q.Id == id);

            if (quiz == null)
            {
                Fehlermeldung = "Das Quiz wurde nicht gefunden.";

                return RedirectToPage();
            }

            string videoTitel = quiz.Schulungsvideo.Titel;

            Context.VideoQuizze.Remove(quiz);

            await Context.SaveChangesAsync();

            Erfolgsmeldung = $"Das Quiz von „{videoTitel}“ wurde gelöscht.";

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostVersucheSpeichernAsync(int id, int? MaximaleVersuche)
        {
            IActionResult? zugriffsErgebnis = await PruefeAdminZugriffAsync();

            if (zugriffsErgebnis != null)
            {
                return zugriffsErgebnis;
            }

            if (MaximaleVersuche.HasValue && (MaximaleVersuche.Value < 1 || MaximaleVersuche.Value > 20))
            {
                Fehlermeldung = "Die Anzahl der Versuche muss zwischen 1 und 20 liegen oder leer bleiben.";

                return RedirectToPage();
            }

            VideoQuiz? quiz = await Context.VideoQuizze.Include(q => q.Schulungsvideo).FirstOrDefaultAsync(q => q.Id == id);

            if (quiz == null)
            {
                Fehlermeldung = "Das Quiz wurde nicht gefunden.";

                return RedirectToPage();
            }

            quiz.MaximaleVersuche = MaximaleVersuche;

            await Context.SaveChangesAsync();

            Erfolgsmeldung = MaximaleVersuche.HasValue
                    ? $"Für „{quiz.Titel}“ sind jetzt {MaximaleVersuche.Value} Versuch(e) erlaubt."
                    : $"Für „{quiz.Titel}“ sind jetzt unbegrenzt viele Versuche erlaubt.";

            return RedirectToPage(new { videoId = quiz.SchulungsvideoId });
        }

        private async Task LadeVideosAsync()
        {
            Videos = await Context.Schulungsvideos.AsNoTracking().Include(v => v.Quiz).OrderBy(v => v.Reihenfolge).ThenBy(v => v.Titel).ToListAsync();
        }
    }
}