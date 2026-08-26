using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using SchulungKK.Models;
using System.Globalization;

namespace SchulungKK.Services
{
    public class ZertifikatService
    {
        private readonly IWebHostEnvironment _environment;

        public ZertifikatService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public byte[] ErstelleZertifikat(QuizErgebnis ergebnis)
        {
            string teilnehmerName = !string.IsNullOrWhiteSpace(ergebnis.Benutzer?.Name) ? ergebnis.Benutzer.Name : ergebnis.Benutzername;

            if (string.IsNullOrWhiteSpace(teilnehmerName))
            {
                teilnehmerName = "Teilnehmer/in";
            }

            string schulungsName = string.IsNullOrWhiteSpace(ergebnis.QuizName) ? "Schulung" : ergebnis.QuizName;

            CultureInfo deutscheKultur = CultureInfo.GetCultureInfo("de-DE");

            using var dokument = new PdfDocument();

            dokument.Info.Title = $"Zertifikat für {teilnehmerName}";

            dokument.Info.Subject = $"Erfolgreicher Abschluss der Schulung {schulungsName}";

            dokument.Info.Author = "SchulungKK";

            PdfPage seite = dokument.AddPage();

            seite.Size = PageSize.A4;
            seite.Orientation = PageOrientation.Landscape;

            using XGraphics grafik = XGraphics.FromPdfPage(seite);

            double breite = seite.Width.Point;
            double hoehe = seite.Height.Point;

            var blau = XColor.FromArgb(0, 102, 204);

            var dunkelblau = XColor.FromArgb(30, 60, 100);

            var gold = XColor.FromArgb(190, 150, 65);

            var hellgrau = XColor.FromArgb(245, 247, 250);

            var zentriert = new XStringFormat
            {
                Alignment = XStringAlignment.Center,
                LineAlignment = XLineAlignment.Near
            };

            // Hintergrund
            grafik.DrawRectangle(new XSolidBrush(XColors.White), 0, 0, breite, hoehe);

            // Äußerer Rahmen
            grafik.DrawRectangle(new XPen(blau, 4),  18, 18, breite - 36, hoehe - 36);

            // Innerer Rahmen
            grafik.DrawRectangle(new XPen(gold, 1.5), 29, 29, breite - 58, hoehe - 58);

            // Dezente Hintergrundfläche
            grafik.DrawRectangle(new XSolidBrush(hellgrau), 42, 42, breite - 84, hoehe - 84);

            // Logo
            ZeichneLogo(grafik, breite);

            var titelSchrift = new XFont("Arial", 32, XFontStyleEx.Bold);

            var untertitelSchrift = new XFont("Arial", 15, XFontStyleEx.Regular);

            var normalSchrift = new XFont("Arial", 16, XFontStyleEx.Regular);

            var schulungsSchrift = ErstellePassendeSchrift( grafik, schulungsName, 22, 14, breite - 180, XFontStyleEx.Bold);

            var kleineSchrift = new XFont("Arial", 11, XFontStyleEx.Regular);

            var kleineFetteSchrift = new XFont("Arial", 11, XFontStyleEx.Bold);

            // Überschrift
            grafik.DrawString("ZERTIFIKAT", titelSchrift, new XSolidBrush(dunkelblau), new XRect( 50, 120, breite - 100, 45), zentriert);

            grafik.DrawLine(new XPen(gold, 2), breite / 2 - 130, 169, breite / 2 + 130, 169);

            // Einleitung
            grafik.DrawString("Hiermit wird bestätigt, dass", untertitelSchrift, XBrushes.Black, new XRect( 50, 190, breite - 100, 30), zentriert);

            // Teilnehmername
            XFont namensSchrift = ErstellePassendeSchrift(grafik, teilnehmerName, 28, 18, breite - 170, XFontStyleEx.Bold);

            grafik.DrawString(teilnehmerName, namensSchrift, new XSolidBrush(blau), new XRect( 60, 225, breite - 120, 42), zentriert);

            // Bestätigungstext
            grafik.DrawString("die folgende Schulung erfolgreich absolviert hat:", normalSchrift, XBrushes.Black, new XRect(50, 278, breite - 100, 30), zentriert);

            // Schulungsname
            grafik.DrawString(schulungsName, schulungsSchrift, new XSolidBrush(dunkelblau), new XRect(75, 320, breite - 150, 40), zentriert);

            // Ergebnisbox
            double boxBreite = 400;
            double boxX = (breite - boxBreite) / 2;

            grafik.DrawRoundedRectangle(new XPen(blau, 1.5), new XSolidBrush(XColors.White), boxX, 378, boxBreite, 66, 8, 8);

            string prozent = ergebnis.Prozent.ToString("0.##", deutscheKultur);

            string ergebnisText = $"{ergebnis.Richtig} von {ergebnis.Gesamt} Fragen richtig";

            grafik.DrawString(ergebnisText, kleineFetteSchrift, XBrushes.Black, new XRect(boxX, 392, boxBreite, 20), zentriert);

            grafik.DrawString($"Ergebnis: {prozent} %", kleineSchrift, new XSolidBrush(dunkelblau), new XRect( boxX, 416, boxBreite, 20), zentriert);

            // Abschlussdatum
            string abschlussdatum = ergebnis.AbgeschlossenAm.ToString("dd.MM.yyyy", deutscheKultur);

            grafik.DrawString($"Abgeschlossen am {abschlussdatum}", normalSchrift, XBrushes.Black, new XRect(50, 465, breite - 100, 25), zentriert);

            // Unterer Bereich
            double linienY = hoehe - 77;

            grafik.DrawLine(new XPen(XColors.Gray, 1), 90, linienY, 300, linienY);

            grafik.DrawLine(new XPen(XColors.Gray, 1), breite - 300, linienY, breite - 90, linienY);

            grafik.DrawString("Ausgestellt durch Kreutzträger Kältetechnik GmbH & Co.", kleineSchrift, XBrushes.Gray, new XRect( 75, linienY + 7, 240, 20), zentriert);

            string zertifikatsNummer = $"SCH-{ergebnis.Id:D6}";

            grafik.DrawString($"Zertifikatsnummer: {zertifikatsNummer}", kleineSchrift, XBrushes.Gray, new XRect(breite - 315, linienY + 7, 240, 20), zentriert);

            using var speicher = new MemoryStream();

            dokument.Save(speicher, false);

            return speicher.ToArray();
        }

        private void ZeichneLogo(XGraphics grafik, double seitenBreite)
        {
            string logoPfad = Path.Combine(_environment.WebRootPath, "images", "kt-logo.png");

            if (!File.Exists(logoPfad))
            {
                return;
            }

            using XImage logo = XImage.FromFile(logoPfad);

            const double maximaleBreite = 230;
            const double maximaleHoehe = 62;

            double faktor = Math.Min(maximaleBreite / logo.PointWidth, maximaleHoehe / logo.PointHeight);

            double logoBreite = logo.PointWidth * faktor;

            double logoHoehe = logo.PointHeight * faktor;

            double logoX = (seitenBreite - logoBreite) / 2;

            grafik.DrawImage(logo, logoX, 48, logoBreite, logoHoehe);
        }

        private static XFont ErstellePassendeSchrift(XGraphics grafik, string text, double maximaleGroesse, double minimaleGroesse, double maximaleBreite, XFontStyleEx schriftStil)
        {
            for (double groesse = maximaleGroesse; groesse >= minimaleGroesse; groesse--)
            {
                var schrift = new XFont("Arial", groesse, schriftStil);

                double textBreite = grafik.MeasureString(text, schrift).Width;

                if (textBreite <= maximaleBreite)
                {
                    return schrift;
                }
            }

            return new XFont("Arial", minimaleGroesse, schriftStil);
        }
    }
}