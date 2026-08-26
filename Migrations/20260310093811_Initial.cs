using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchulungKK.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Benutzer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Benutzername = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Passwort = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    RegistriertAm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LetzterLogin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Aktiv = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Benutzer", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QuizErgebnisse",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BenutzerId = table.Column<int>(type: "int", nullable: false),
                    QuizName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Richtig = table.Column<int>(type: "int", nullable: false),
                    Gesamt = table.Column<int>(type: "int", nullable: false),
                    Prozent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Bestanden = table.Column<bool>(type: "bit", nullable: false),
                    AbgeschlossenAm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizErgebnisse", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuizErgebnisse_Benutzer_BenutzerId",
                        column: x => x.BenutzerId,
                        principalTable: "Benutzer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Benutzer_Benutzername",
                table: "Benutzer",
                column: "Benutzername",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuizErgebnisse_BenutzerId",
                table: "QuizErgebnisse",
                column: "BenutzerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuizErgebnisse");

            migrationBuilder.DropTable(
                name: "Benutzer");
        }
    }
}
