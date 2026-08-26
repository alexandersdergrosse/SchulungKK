using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchulungKK.Migrations
{
    /// <inheritdoc />
    public partial class MaximaleVersucheUmbenennen : Migration
    {
        /// <inheritdoc />
        protected override void Up(
            MigrationBuilder migrationBuilder)
        {
            /*
             * Zuerst wird die vorhandene Datenbankspalte
             * umbenannt.
             *
             * Die Daten bleiben dabei vollständig erhalten.
             */
            migrationBuilder.RenameColumn(
                name: "MaximaleWiederholungen",
                table: "VideoQuizze",
                newName: "MaximaleVersuche");

            /*
             * Bisher bedeutete der gespeicherte Wert:
             *
             * 2 = zwei Wiederholungen
             *     + ein erster Versuch
             *     = drei Versuche insgesamt.
             *
             * Jetzt soll der Wert direkt die Anzahl
             * aller erlaubten Versuche darstellen.
             *
             * Deshalb erhöhen wir vorhandene Werte
             * einmalig um 1.
             *
             * NULL bleibt NULL und bedeutet weiterhin
             * unbegrenzt.
             */
            migrationBuilder.Sql(
                """
                UPDATE [VideoQuizze]
                SET [MaximaleVersuche] =
                    [MaximaleVersuche] + 1
                WHERE [MaximaleVersuche] IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(
            MigrationBuilder migrationBuilder)
        {
            /*
             * Beim Zurücksetzen muss die Umrechnung
             * wieder rückgängig gemacht werden.
             */
            migrationBuilder.Sql(
                """
                UPDATE [VideoQuizze]
                SET [MaximaleVersuche] =
                    [MaximaleVersuche] - 1
                WHERE [MaximaleVersuche] IS NOT NULL;
                """);

            migrationBuilder.RenameColumn(
                name: "MaximaleVersuche",
                table: "VideoQuizze",
                newName: "MaximaleWiederholungen");
        }
    }
}