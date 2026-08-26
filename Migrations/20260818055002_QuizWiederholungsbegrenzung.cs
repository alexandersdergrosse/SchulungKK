using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchulungKK.Migrations
{
    /// <inheritdoc />
    public partial class QuizWiederholungsbegrenzung : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaximaleWiederholungen",
                table: "VideoQuizze",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaximaleWiederholungen",
                table: "VideoQuizze");
        }
    }
}
