using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchulungKK.Migrations
{
    /// <inheritdoc />
    public partial class WordQuizProVideo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VideoQuizId",
                table: "QuizErgebnisse",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "VideoQuizze",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchulungsvideoId = table.Column<int>(type: "int", nullable: false),
                    Titel = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Beschreibung = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Bestehensgrenze = table.Column<int>(type: "int", nullable: false),
                    FragenAnzahl = table.Column<int>(type: "int", nullable: false),
                    InhaltJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quelldateiname = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ErstelltAm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AktualisiertAm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoQuizze", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoQuizze_Schulungsvideos_SchulungsvideoId",
                        column: x => x.SchulungsvideoId,
                        principalTable: "Schulungsvideos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuizErgebnisse_VideoQuizId",
                table: "QuizErgebnisse",
                column: "VideoQuizId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoQuizze_SchulungsvideoId",
                table: "VideoQuizze",
                column: "SchulungsvideoId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_QuizErgebnisse_VideoQuizze_VideoQuizId",
                table: "QuizErgebnisse",
                column: "VideoQuizId",
                principalTable: "VideoQuizze",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuizErgebnisse_VideoQuizze_VideoQuizId",
                table: "QuizErgebnisse");

            migrationBuilder.DropTable(
                name: "VideoQuizze");

            migrationBuilder.DropIndex(
                name: "IX_QuizErgebnisse_VideoQuizId",
                table: "QuizErgebnisse");

            migrationBuilder.DropColumn(
                name: "VideoQuizId",
                table: "QuizErgebnisse");
        }
    }
}
