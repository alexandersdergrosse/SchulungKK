using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchulungKK.Data;
using SchulungKK.Models;

namespace SchulungKK.Pages
{
    public class IndexModel : PageModel
    {
        private static readonly JsonSerializerOptions JsonOptionen = new(JsonSerializerDefaults.Web);

        private readonly SchulungenDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public IndexModel(SchulungenDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public string CurrentUsername { get; set; } = string.Empty;

        public List<VideoAnzeige> Videos { get; set; } = new();

        public VideoAnzeige? AusgewaehltesVideo { get; set; }

        public int AusgewaehltesVideoPosition { get; set; }

        public int FehlendeVideosAnzahl { get; set; }

        public async Task<IActionResult> OnGetAsync(int? videoId)
        {
            if (!VersucheBenutzerIdZuLesen(out int benutzerId))
            {
                return RedirectToPage("/Anmelden");
            }

            Benutzer? benutzer = await _context.Benutzer.AsNoTracking().FirstOrDefaultAsync(b => b.Id == benutzerId && b.Aktiv);

            if (benutzer == null)
            {
                HttpContext.Session.Clear();

                return RedirectToPage("/Anmelden");
            }

            CurrentUsername = benutzer.Benutzername;

            ViewData["Username"] = CurrentUsername;

            /*
             * Die Gruppenfilterung gilt auch für
             * Administratoren.
             */
            IQueryable<Schulungsvideo> videoAbfrage = _context.Schulungsvideos.AsNoTracking().Include(v => v.Quiz).Where(video => video.Aktiv && video.GruppeVideos
                .Any(gruppeVideo => gruppeVideo.Gruppe.Aktiv && gruppeVideo.Gruppe.BenutzerGruppen.Any(benutzerGruppe => benutzerGruppe.BenutzerId == benutzer.Id)));

            List<Schulungsvideo> zugewieseneVideos = await videoAbfrage.OrderBy(v => v.Reihenfolge).ThenBy(v => v.Titel).ToListAsync();

            string videoOrdner = Path.Combine(_environment.WebRootPath, "videos");

            Directory.CreateDirectory(videoOrdner);

            foreach (Schulungsvideo video in zugewieseneVideos)
            {
                string sichererDateiname = Path.GetFileName(video.Dateiname);

                string dateiPfad = Path.Combine(videoOrdner, sichererDateiname);

                if (!System.IO.File.Exists(dateiPfad))
                {
                    FehlendeVideosAnzahl++;
                    continue;
                }

                Videos.Add(
                    new VideoAnzeige
                    {
                        Id = video.Id,
                        Titel = video.Titel,
                        Beschreibung = video.Beschreibung,

                        Dateiname = sichererDateiname,

                        VideoUrl = "/videos/" + Uri.EscapeDataString(sichererDateiname),

                        ContentType = ErmittleContentType(sichererDateiname),

                        HatQuiz = video.Quiz != null
                    });
            }

            if (videoId.HasValue)
            {
                AusgewaehltesVideo = Videos.FirstOrDefault(video => video.Id == videoId.Value);
            }

            /*
             * Wurde keine Video-ID übermittelt oder besitzt
             * der Benutzer keinen Zugriff auf diese ID,
             * wird das erste verfügbare Video ausgewählt.
             */
            AusgewaehltesVideo ??= Videos.FirstOrDefault();

            if (AusgewaehltesVideo != null)
            {
                AusgewaehltesVideoPosition = Videos.FindIndex(video => video.Id == AusgewaehltesVideo.Id) + 1;
            }

            return Page();
        }

        /*
         * Liefert das Quiz für ein Video.
         *
         * Die richtigen Antworten werden bewusst
         * nicht an den Browser übertragen.
         */
        public async Task<IActionResult> OnGetQuizAsync(int videoId)
        {
            if (!VersucheBenutzerIdZuLesen(out int benutzerId))
            {
                return JsonFehler("Du bist nicht angemeldet.", StatusCodes.Status401Unauthorized);
            }

            bool benutzerIstAktiv = await _context.Benutzer.AsNoTracking().AnyAsync(b => b.Id == benutzerId && b.Aktiv);

            if (!benutzerIstAktiv)
            {
                HttpContext.Session.Clear();

                return JsonFehler("Dein Benutzerkonto ist nicht aktiv.", StatusCodes.Status401Unauthorized);
            }

            /*
             * Quiz laden.
             *
             * Gleichzeitig wird geprüft, ob der Benutzer
             * über eine seiner aktiven Gruppen Zugriff auf
             * das zugehörige Video besitzt.
             */
            var quiz =
                await _context.VideoQuizze
                    .AsNoTracking()
                    .Where(q =>
                        q.SchulungsvideoId == videoId &&
                        q.Schulungsvideo.Aktiv &&
                        q.Schulungsvideo.GruppeVideos.Any(gruppeVideo => gruppeVideo.Gruppe.Aktiv && gruppeVideo.Gruppe.BenutzerGruppen.Any(benutzerGruppe => benutzerGruppe.BenutzerId == benutzerId)))
                    .Select(q => new
                    {
                        q.Id,
                        q.SchulungsvideoId,
                        q.Titel,
                        q.Beschreibung,
                        q.Bestehensgrenze,
                        q.MaximaleVersuche,
                        q.InhaltJson
                    })
                    .FirstOrDefaultAsync();

            if (quiz == null)
            {
                return JsonFehler("Für dieses Video ist kein zugängliches Quiz vorhanden.", StatusCodes.Status404NotFound);
            }

            /*
             * Bisherige Quizversuche dieses Benutzers
             * für genau dieses Quiz zählen.
             */
            int bisherigeVersuche = await _context.QuizErgebnisse.AsNoTracking().CountAsync(ergebnis => ergebnis.BenutzerId == benutzerId && ergebnis.VideoQuizId == quiz.Id);

            /*
             * Sobald ein Quiz bestanden wurde,
             * ist keine weitere Teilnahme erforderlich.
             */
            bool bereitsBestanden = await _context.QuizErgebnisse.AsNoTracking().AnyAsync(ergebnis => ergebnis.BenutzerId == benutzerId && ergebnis.VideoQuizId == quiz.Id && ergebnis.Bestanden);

            if (bereitsBestanden)
            {
                return new JsonResult(
                    new
                    {
                        success = true,
                        darfTeilnehmen = false,
                        bereitsBestanden = true,

                        message = "Du hast dieses Quiz bereits bestanden."
                    });
            }

            /*
             * MaximaleVersuche bezeichnet nur
             * Wiederholungen nach dem ersten Versuch.
             *
             * Beispiel:
             * 2 Wiederholungen = 3 Versuche insgesamt.
             *
             * NULL bedeutet unbegrenzt.
             */
            int? maximaleVersuche = quiz.MaximaleVersuche;

            if (maximaleVersuche.HasValue && bisherigeVersuche >= maximaleVersuche.Value)
            {
                return new JsonResult(
                    new
                    {
                        success = true,
                        darfTeilnehmen = false,
                        versucheAufgebraucht = true,

                        message = $"Du hast alle {maximaleVersuche.Value} " + "verfügbaren Versuche verwendet."
                    });
            }

            /*
             * Erst jetzt werden die eigentlichen Fragen
             * aus dem gespeicherten JSON gelesen.
             */
            QuizDaten? quizDaten = DeserialisiereQuiz(quiz.InhaltJson);

            if (quizDaten == null || quizDaten.Fragen.Count == 0)
            {
                return JsonFehler("Das Quiz konnte nicht gelesen werden.", StatusCodes.Status500InternalServerError);
            }

            /*
             * Wichtig:
             * Die richtige Antwort wird NICHT an den
             * Browser übertragen.
             *
             * RichtigeAntwortIndex bleibt ausschließlich
             * auf dem Server.
             */
            var fragen = quizDaten.Fragen.Select((frage, frageIndex) => new { frageIndex, text = frage.Text, antworten = frage.Antworten }).ToList();

            /*
             * Erst nachdem "fragen" erstellt wurde,
             * darf die Variable hier verwendet werden.
             */
            return new JsonResult(
                new
                {
                    success = true,
                    darfTeilnehmen = true,

                    quizId = quiz.Id,

                    videoId = quiz.SchulungsvideoId,

                    titel = quiz.Titel,

                    beschreibung = quiz.Beschreibung,

                    bestehensgrenze = quiz.Bestehensgrenze,

                    maximaleVersuche = quiz.MaximaleVersuche,

                    bisherigeVersuche,

                    fragen
                });
        }

        /*
         * Wertet die Antworten serverseitig aus.
         * Der Browser übermittelt nur die gewählten
         * Antwortnummern.
         */
        public async Task<IActionResult> OnPostQuizAuswertenAsync([FromBody] QuizAbgabe eingabe)
        {
            if (!VersucheBenutzerIdZuLesen(out int benutzerId))
            {
                return JsonFehler("Du bist nicht angemeldet.", StatusCodes.Status401Unauthorized);
            }

            Benutzer? benutzer = await _context.Benutzer.AsNoTracking().FirstOrDefaultAsync(b => b.Id == benutzerId && b.Aktiv);

            if (benutzer == null)
            {
                HttpContext.Session.Clear();

                return JsonFehler("Dein Benutzerkonto ist nicht aktiv.", StatusCodes.Status401Unauthorized);
            }

            if (eingabe.VideoQuizId <= 0)
            {
                return JsonFehler("Es wurde kein gültiges Quiz übermittelt.", StatusCodes.Status400BadRequest);
            }

            var quiz = await _context.VideoQuizze.AsNoTracking().Where(q => q.Id == eingabe.VideoQuizId && q.Schulungsvideo.Aktiv &&
                        q.Schulungsvideo.GruppeVideos.Any(gruppeVideo => gruppeVideo.Gruppe.Aktiv && gruppeVideo.Gruppe.BenutzerGruppen.Any(benutzerGruppe => benutzerGruppe.BenutzerId == benutzerId)))
                    .Select(q => new
                    {
                        q.Id,
                        q.Titel,
                        q.Bestehensgrenze,
                        q.MaximaleVersuche,
                        q.InhaltJson
                    }).FirstOrDefaultAsync();

            if (quiz == null)
            {
                return JsonFehler("Das Quiz wurde nicht gefunden oder du besitzt keinen Zugriff darauf.", StatusCodes.Status404NotFound);
            }

            int bisherigeVersuche = await _context.QuizErgebnisse.AsNoTracking().CountAsync(ergebnis => ergebnis.BenutzerId == benutzerId && ergebnis.VideoQuizId == quiz.Id);

            bool bereitsBestanden = await _context.QuizErgebnisse.AsNoTracking().AnyAsync(ergebnis => ergebnis.BenutzerId == benutzerId && ergebnis.VideoQuizId == quiz.Id && ergebnis.Bestanden);

            if (bereitsBestanden)
            {
                return JsonFehler("Du hast dieses Quiz bereits bestanden.", StatusCodes.Status409Conflict);
            }

            int? maximaleVersuche = quiz.MaximaleVersuche;

            if (maximaleVersuche.HasValue && bisherigeVersuche >= maximaleVersuche.Value)
            {
                return JsonFehler("Du hast keine weiteren Quizversuche mehr.", StatusCodes.Status409Conflict);
            }

            QuizDaten? quizDaten = DeserialisiereQuiz(quiz.InhaltJson);

            if (quizDaten == null || quizDaten.Fragen.Count == 0)
            {
                return JsonFehler("Das Quiz konnte nicht gelesen werden.", StatusCodes.Status500InternalServerError);
            }

            if (eingabe.Antworten == null || eingabe.Antworten.Count != quizDaten.Fragen.Count)
            {
                return JsonFehler("Bitte beantworte alle Fragen.", StatusCodes.Status400BadRequest);
            }

            var abgegebeneAntworten = new Dictionary<int, int>();

            foreach (QuizAntwortEingabe antwort in eingabe.Antworten)
            {
                if (antwort.FrageIndex < 0 || antwort.FrageIndex >= quizDaten.Fragen.Count)
                {
                    return JsonFehler( "Mindestens eine Frage ist ungültig.", StatusCodes.Status400BadRequest);
                }

                if (!abgegebeneAntworten.TryAdd(antwort.FrageIndex, antwort.AntwortIndex))
                {
                    return JsonFehler("Mindestens eine Frage wurde mehrfach übermittelt.", StatusCodes.Status400BadRequest);
                }

                QuizFrage frage = quizDaten.Fragen[antwort.FrageIndex];

                if (antwort.AntwortIndex < 0 || antwort.AntwortIndex >= frage.Antworten.Count)
                {
                    return JsonFehler("Mindestens eine Antwort ist ungültig.", StatusCodes.Status400BadRequest);
                }
            }

            int richtigeAntworten = 0;

            for (int frageIndex = 0; frageIndex < quizDaten.Fragen.Count; frageIndex++)
            {
                if (!abgegebeneAntworten.TryGetValue(frageIndex, out int antwortIndex))
                {
                    return JsonFehler("Bitte beantworte alle Fragen.", StatusCodes.Status400BadRequest);
                }

                if (antwortIndex == quizDaten.Fragen[frageIndex].RichtigeAntwortIndex)
                {
                    richtigeAntworten++;
                }
            }

            int gesamt = quizDaten.Fragen.Count;

            decimal prozent = Math.Round((decimal)richtigeAntworten / gesamt * 100, 2);

            bool bestanden = prozent >= quiz.Bestehensgrenze;

            string quizName = quiz.Titel.Length > 100 ? quiz.Titel[..100] : quiz.Titel;

            var quizErgebnis = new QuizErgebnis
                {
                    BenutzerId = benutzer.Id,

                    VideoQuizId = quiz.Id,

                    Benutzername = benutzer.Benutzername,

                    QuizName = quizName,

                    Richtig = richtigeAntworten,

                    Gesamt = gesamt,

                    Prozent = prozent,

                    Bestanden = bestanden,

                    AbgeschlossenAm = DateTime.Now
                };

            _context.QuizErgebnisse.Add(quizErgebnis);

            await _context.SaveChangesAsync();

            int verwendeteVersuche = bisherigeVersuche + 1;

            int? verbleibendeVersuche = maximaleVersuche.HasValue ? Math.Max(0, maximaleVersuche.Value - verwendeteVersuche) : null;

            string? zertifikatUrl = null;

            if (bestanden)
            {
                zertifikatUrl = Url.Page("/Ergebnisse","Zertifikat", new { id = quizErgebnis.Id });
            }

            return new JsonResult(
                new
                {
                    success = true,
                    bestanden,
                    richtigeAntworten,
                    gesamt,
                    prozent,
                    bestehensgrenze = quiz.Bestehensgrenze,

                    verbleibendeVersuche,
                    maximaleVersuche = quiz.MaximaleVersuche,

                    ergebnisId = quizErgebnis.Id,

                    zertifikatUrl
                });
        }

        // Zertifikat für Videos OHNE Quiz direkt ausstellen
        public async Task<IActionResult> OnPostVideoAbgeschlossenAsync([FromBody] VideoAbschlussEingabe eingabe)
        {
            if (!VersucheBenutzerIdZuLesen(out int benutzerId))
            {
                return JsonFehler("Du bist nicht angemeldet.", StatusCodes.Status401Unauthorized);
            }

            Benutzer? benutzer = await _context.Benutzer.AsNoTracking().FirstOrDefaultAsync(b => b.Id == benutzerId && b.Aktiv);

            if (benutzer == null)
            {
                HttpContext.Session.Clear();
                return JsonFehler("Dein Benutzerkonto ist nicht aktiv.", StatusCodes.Status401Unauthorized);
            }

            // Prüfen ob das Video existiert, der Benutzer Zugriff hat UND kein Quiz vorhanden ist
            var video = await _context.Schulungsvideos.AsNoTracking().Where(v => v.Id == eingabe.VideoId && v.Aktiv && v.Quiz == null // Nur Videos OHNE Quiz!
            && v.GruppeVideos.Any(gv => gv.Gruppe.Aktiv && gv.Gruppe.BenutzerGruppen.Any(bg => bg.BenutzerId == benutzerId))).FirstOrDefaultAsync();

            if (video == null)
            {
                return JsonFehler("Video nicht gefunden oder Quiz vorhanden.", StatusCodes.Status404NotFound);
            }

            // Wurde das Zertifikat bereits ausgestellt?
            bool bereitsAusgestellt = await _context.QuizErgebnisse.AsNoTracking().AnyAsync(e => e.BenutzerId == benutzerId && e.VideoQuizId == null && e.QuizName == video.Titel);

            if (bereitsAusgestellt)
            {
                return new JsonResult(new
                {
                    success = true,
                    bereitsAusgestellt = true,
                    message = "Du hast für dieses Video bereits ein Zertifikat erhalten."
                });
            }

            // Zertifikat-Eintrag erstellen
            var ergebnis = new QuizErgebnis
            {
                BenutzerId = benutzer.Id,
                VideoQuizId = null,
                Benutzername = benutzer.Benutzername,
                QuizName = video.Titel,
                Richtig = 1,
                Gesamt = 1,
                Prozent = 100,
                Bestanden = true,
                AbgeschlossenAm = DateTime.Now
            };

            _context.QuizErgebnisse.Add(ergebnis);
            await _context.SaveChangesAsync();

            string? zertifikatUrl = Url.Page("/Ergebnisse", "Zertifikat", new { id = ergebnis.Id });

            return new JsonResult(new
            {
                success = true,
                bereitsAusgestellt = false,
                zertifikatUrl
            });
        }

        // Neue Klasse ganz unten in der IndexModel Klasse hinzufügen:
        public class VideoAbschlussEingabe
        {
            public int VideoId { get; set; }
        }


        private bool VersucheBenutzerIdZuLesen(out int benutzerId)
        {
            benutzerId = 0;

            string? istAngemeldet = HttpContext.Session.GetString("IsLoggedIn");

            string? benutzerIdText = HttpContext.Session.GetString("UserId");

            return istAngemeldet == "true" && int.TryParse( benutzerIdText, out benutzerId);
        }

        private static QuizDaten?
            DeserialisiereQuiz(string inhaltJson)
        {
            try
            {
                return JsonSerializer.Deserialize<QuizDaten>(inhaltJson, JsonOptionen);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static JsonResult JsonFehler(string nachricht, int statusCode)
        {
            return new JsonResult(new { success = false, message = nachricht })
            {
                StatusCode = statusCode
            };
        }

        private static string ErmittleContentType(string dateiname)
        {
            string dateiendung = Path.GetExtension(dateiname).ToLowerInvariant();

            return dateiendung switch
            {
                ".mp4" => "video/mp4",
                ".webm" => "video/webm",
                ".ogg" => "video/ogg",
                _ => "application/octet-stream"
            };
        }

        public class QuizAbgabe
        {
            public int VideoQuizId { get; set; }

            public List<QuizAntwortEingabe> Antworten { get; set; } = new();
        }

        public class QuizAntwortEingabe
        {
            public int FrageIndex { get; set; }

            public int AntwortIndex { get; set; }
        }

        public class VideoAnzeige
        {
            public int Id { get; set; }

            public string Titel { get; set; } = string.Empty;

            public string? Beschreibung { get; set; }

            public string Dateiname { get; set; } = string.Empty;

            public string VideoUrl { get; set; } = string.Empty;

            public string ContentType { get; set; } = string.Empty;

            public bool HatQuiz { get; set; }
        }
    }
}