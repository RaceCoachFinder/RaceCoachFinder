using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class WedstrijdVelden : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WedstrijdNaam",
                table: "TrainingVoorstellen",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WedstrijdOrganisatie",
                table: "TrainingVoorstellen",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WedstrijdNaam",
                table: "CoachAanboden",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WedstrijdOrganisatie",
                table: "CoachAanboden",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WedstrijdNaam",
                table: "TrainingVoorstellen");

            migrationBuilder.DropColumn(
                name: "WedstrijdOrganisatie",
                table: "TrainingVoorstellen");

            migrationBuilder.DropColumn(
                name: "WedstrijdNaam",
                table: "CoachAanboden");

            migrationBuilder.DropColumn(
                name: "WedstrijdOrganisatie",
                table: "CoachAanboden");
        }
    }
}
