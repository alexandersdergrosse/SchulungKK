using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchulungKK.Migrations
{
    /// <inheritdoc />
    public partial class GruppenUndVideoverwaltung : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "QuizName",
                table: "QuizErgebnisse",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Prozent",
                table: "QuizErgebnisse",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<string>(
                name: "Benutzername",
                table: "QuizErgebnisse",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<bool>(
                name: "IstAdmin",
                table: "Benutzer",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Gruppen",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Beschreibung = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Aktiv = table.Column<bool>(type: "bit", nullable: false),
                    ErstelltAm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gruppen", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Schulungsvideos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Titel = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Dateiname = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Beschreibung = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Aktiv = table.Column<bool>(type: "bit", nullable: false),
                    Reihenfolge = table.Column<int>(type: "int", nullable: false),
                    ErstelltAm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schulungsvideos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BenutzerGruppen",
                columns: table => new
                {
                    BenutzerId = table.Column<int>(type: "int", nullable: false),
                    GruppeId = table.Column<int>(type: "int", nullable: false),
                    ZugeordnetAm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenutzerGruppen", x => new { x.BenutzerId, x.GruppeId });
                    table.ForeignKey(
                        name: "FK_BenutzerGruppen_Benutzer_BenutzerId",
                        column: x => x.BenutzerId,
                        principalTable: "Benutzer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BenutzerGruppen_Gruppen_GruppeId",
                        column: x => x.GruppeId,
                        principalTable: "Gruppen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GruppeVideos",
                columns: table => new
                {
                    GruppeId = table.Column<int>(type: "int", nullable: false),
                    SchulungsvideoId = table.Column<int>(type: "int", nullable: false),
                    ZugeordnetAm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GruppeVideos", x => new { x.GruppeId, x.SchulungsvideoId });
                    table.ForeignKey(
                        name: "FK_GruppeVideos_Gruppen_GruppeId",
                        column: x => x.GruppeId,
                        principalTable: "Gruppen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GruppeVideos_Schulungsvideos_SchulungsvideoId",
                        column: x => x.SchulungsvideoId,
                        principalTable: "Schulungsvideos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BenutzerGruppen_GruppeId",
                table: "BenutzerGruppen",
                column: "GruppeId");

            migrationBuilder.CreateIndex(
                name: "IX_Gruppen_Name",
                table: "Gruppen",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GruppeVideos_SchulungsvideoId",
                table: "GruppeVideos",
                column: "SchulungsvideoId");

            migrationBuilder.CreateIndex(
                name: "IX_Schulungsvideos_Dateiname",
                table: "Schulungsvideos",
                column: "Dateiname",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BenutzerGruppen");

            migrationBuilder.DropTable(
                name: "GruppeVideos");

            migrationBuilder.DropTable(
                name: "Gruppen");

            migrationBuilder.DropTable(
                name: "Schulungsvideos");

            migrationBuilder.DropColumn(
                name: "IstAdmin",
                table: "Benutzer");

            migrationBuilder.AlterColumn<string>(
                name: "QuizName",
                table: "QuizErgebnisse",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<decimal>(
                name: "Prozent",
                table: "QuizErgebnisse",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "Benutzername",
                table: "QuizErgebnisse",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);
        }
    }
}
