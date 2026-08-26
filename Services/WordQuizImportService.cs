using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using SchulungKK.Models;

namespace SchulungKK.Services
{
    public class WordQuizImportService
    {
        private const int MaximaleFragenAnzahl = 100;

        public async Task<WordQuizImportErgebnis>
            ImportiereAsync(IFormFile datei)
        {
            await using MemoryStream speicher = new MemoryStream();

            await datei.CopyToAsync(speicher);

            speicher.Position = 0;

            try
            {
                using WordprocessingDocument dokument = WordprocessingDocument.Open(speicher, false);

                MainDocumentPart? hauptteil = dokument.MainDocumentPart;

                Body? dokumentInhalt = hauptteil?.Document?.Body;

                if (dokumentInhalt == null)
                {
                    throw new QuizImportException("Das Word-Dokument enthält keinen lesbaren Inhalt.");
                }

                if (dokumentInhalt == null)
                {
                    throw new QuizImportException("Das Word-Dokument enthält keinen lesbaren Inhalt.");
                }

                List<Table> tabellen = dokumentInhalt.Elements<Table>().ToList();

                if (tabellen.Count < 2)
                {
                    throw new QuizImportException("Das Word-Dokument muss mindestens zwei Tabellen enthalten.");
                }

                Metadaten metadaten = LeseMetadaten(tabellen[0]);

                QuizDaten quizDaten =LeseFragen(tabellen[1]);

                return new WordQuizImportErgebnis
                {
                    Titel = metadaten.Titel,
                    Beschreibung = metadaten.Beschreibung,

                    Bestehensgrenze = metadaten.Bestehensgrenze,

                    QuizDaten = quizDaten
                };
            }
            catch (QuizImportException)
            {
                throw;
            }
            catch (Exception exception)
                when (exception is IOException || exception is InvalidDataException || exception is ArgumentException || exception is OpenXmlPackageException)
            {
                throw new QuizImportException("Die Word-Datei ist beschädigt oder besitzt kein gültiges DOCX-Format.");
            }
        }

        private static Metadaten LeseMetadaten(Table tabelle)
        {
            List<TableRow> zeilen = tabelle.Elements<TableRow>().ToList();

            if (zeilen.Count < 4)
            {
                throw new QuizImportException("Die erste Tabelle enthält nicht alle erforderlichen Quizdaten.");
            }

            string titel = string.Empty;

            string? beschreibung = null;

            int? bestehensgrenze = null;

            foreach (TableRow zeile in zeilen.Skip(1))
            {
                List<string> zellen = LeseZellen(zeile);

                if (zellen.Count < 2)
                {
                    continue;
                }

                string schluessel = NormalisiereSchluessel(zellen[0]);

                string wert = BereinigeText(zellen[1]);

                switch (schluessel)
                {
                    case "quiztitel":
                    case "titel":
                        titel = wert;
                        break;

                    case "beschreibung":
                        beschreibung = string.IsNullOrWhiteSpace(wert) ? null : wert;
                        break;

                    case "bestehensgrenze":
                        if (int.TryParse(wert, out int grenze))
                        {
                            bestehensgrenze = grenze;
                        }

                        break;
                }
            }

            if (titel.Length < 2 || titel.Length > 150)
            {
                throw new QuizImportException("Der Quiztitel muss zwischen 2 und 150 Zeichen lang sein.");
            }

            if (beschreibung?.Length > 500)
            {
                throw new QuizImportException("Die Quizbeschreibung darf höchstens 500 Zeichen lang sein.");
            }

            if (!bestehensgrenze.HasValue || bestehensgrenze.Value < 1 || bestehensgrenze.Value > 100)
            {
                throw new QuizImportException("Die Bestehensgrenze muss eine ganze Zahl zwischen 1 und 100 sein.");
            }

            return new Metadaten
            {
                Titel = titel,
                Beschreibung = beschreibung,

                Bestehensgrenze = bestehensgrenze.Value
            };
        }

        private static QuizDaten LeseFragen(Table tabelle)
        {
            List<TableRow> zeilen = tabelle.Elements<TableRow>().ToList();

            if (zeilen.Count < 2)
            {
                throw new QuizImportException("Die Fragentabelle enthält keine Fragen.");
            }

            PruefeKopfzeile(zeilen[0]);

            var quizDaten = new QuizDaten();

            for (int zeilenIndex = 1; zeilenIndex < zeilen.Count; zeilenIndex++)
            {
                List<string> zellen = LeseZellen(zeilen[zeilenIndex]);

                if (zellen.All(string.IsNullOrWhiteSpace))
                {
                    continue;
                }

                int fragenNummer = zeilenIndex;

                if (zellen.Count != 6)
                {
                    throw new QuizImportException($"Frage {fragenNummer} besitzt nicht genau sechs Tabellenspalten.");
                }

                string frage = BereinigeText(zellen[0]);

                if (frage.Length < 2 || frage.Length > 500)
                {
                    throw new QuizImportException($"Frage {fragenNummer} muss zwischen 2 und 500 Zeichen lang sein.");
                }

                string[] antwortFelder =
                {
                    BereinigeText(zellen[1]),
                    BereinigeText(zellen[2]),
                    BereinigeText(zellen[3]),
                    BereinigeText(zellen[4])
                };

                if (string.IsNullOrWhiteSpace(antwortFelder[0]) || string.IsNullOrWhiteSpace(antwortFelder[1]))
                {
                    throw new QuizImportException($"Frage {fragenNummer} benötigt mindestens Antwort A und Antwort B.");
                }

                if (string.IsNullOrWhiteSpace(antwortFelder[2]) && !string.IsNullOrWhiteSpace(antwortFelder[3]))
                {
                    throw new QuizImportException($"Bei Frage {fragenNummer} darf Antwort D nicht gefüllt sein, wenn Antwort C leer ist.");
                }

                List<string> antworten = antwortFelder.TakeWhile(antwort => !string.IsNullOrWhiteSpace(antwort)).ToList();

                for (int antwortIndex = 0; antwortIndex < antworten.Count; antwortIndex++)
                {
                    if (antworten[antwortIndex].Length > 300)
                    {
                        throw new QuizImportException( $"Antwort {antwortIndex + 1} bei Frage {fragenNummer} ist länger als 300 Zeichen.");
                    }
                }

                if (antworten.Distinct(StringComparer.OrdinalIgnoreCase).Count() != antworten.Count)
                {
                    throw new QuizImportException($"Frage {fragenNummer} enthält doppelte Antwortmöglichkeiten.");
                }

                int richtigeAntwortIndex = LeseRichtigeAntwort(zellen[5], fragenNummer);

                if (richtigeAntwortIndex >= antworten.Count)
                {
                    throw new QuizImportException($"Die richtige Antwort bei Frage {fragenNummer} verweist auf ein leeres Antwortfeld.");
                }

                quizDaten.Fragen.Add(
                    new QuizFrage
                    {
                        Text = frage,
                        Antworten = antworten,

                        RichtigeAntwortIndex = richtigeAntwortIndex
                    });

                if (quizDaten.Fragen.Count > MaximaleFragenAnzahl)
                {
                    throw new QuizImportException($"Ein Quiz darf höchstens {MaximaleFragenAnzahl} Fragen enthalten.");
                }
            }

            if (quizDaten.Fragen.Count == 0)
            {
                throw new QuizImportException("Die Word-Datei enthält keine gültigen Fragen.");
            }

            return quizDaten;
        }

        private static void PruefeKopfzeile(TableRow kopfzeile)
        {
            List<string> zellen = LeseZellen(kopfzeile).Select(NormalisiereSchluessel).ToList();

            string[] erwarteteSpalten =
            {
                "frage",
                "antworta",
                "antwortb",
                "antwortc",
                "antwortd",
                "richtigeantwort"
            };

            if (zellen.Count != erwarteteSpalten.Length || !zellen.SequenceEqual(erwarteteSpalten))
            {
                throw new QuizImportException("Die Spaltenüberschriften der Fragentabelle wurden verändert.");
            }
        }

        private static int LeseRichtigeAntwort(string wert, int fragenNummer)
        {
            string normalisiert = BereinigeText(wert).ToUpperInvariant();

            int index = normalisiert switch
            {
                "A" => 0,
                "B" => 1,
                "C" => 2,
                "D" => 3,
                "1" => 0,
                "2" => 1,
                "3" => 2,
                "4" => 3,
                _ => -1
            };

            if (index < 0)
            {
                throw new QuizImportException($"Die richtige Antwort bei Frage {fragenNummer} muss A, B, C, D oder 1, 2, 3, 4 sein.");
            }

            return index;
        }

        private static List<string> LeseZellen(TableRow zeile)
        {
            return zeile.Elements<TableCell>().Select(LeseZellenText).ToList();
        }

        private static string LeseZellenText(TableCell zelle)
        {
            IEnumerable<string> absatzTexte = zelle.Elements<Paragraph>().Select(absatz => string.Concat(absatz.Descendants<Text>().Select(text => text.Text)));

            return BereinigeText(string.Join(Environment.NewLine, absatzTexte));
        }

        private static string BereinigeText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            return string.Join(" ", text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        }

        private static string NormalisiereSchluessel(string text)
        {
            return new string(BereinigeText(text).Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
        }

        private class Metadaten
        {
            public string Titel { get; set; } = string.Empty;

            public string? Beschreibung { get; set; }

            public int Bestehensgrenze { get; set; }
        }
    }

    public class WordQuizImportErgebnis
    {
        public string Titel { get; set; } = string.Empty;

        public string? Beschreibung { get; set; }

        public int Bestehensgrenze { get; set; }

        public QuizDaten QuizDaten { get; set; } = new();
    }

    public class QuizImportException : Exception
    {
        public QuizImportException(string nachricht) : base(nachricht)
        {
        }
    }
}