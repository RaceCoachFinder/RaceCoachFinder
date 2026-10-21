using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class TrainingVoorstellen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrainingVoorstellen",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GroepsgesprekId = table.Column<int>(type: "INTEGER", nullable: false),
                    RijderAanvraagId = table.Column<int>(type: "INTEGER", nullable: false),
                    OrganisatorId = table.Column<string>(type: "TEXT", nullable: false),
                    Titel = table.Column<string>(type: "TEXT", nullable: false),
                    Beschrijving = table.Column<string>(type: "TEXT", nullable: false),
                    Locatie = table.Column<string>(type: "TEXT", nullable: false),
                    Datum = table.Column<DateTime>(type: "TEXT", nullable: true),
                    MaxRijders = table.Column<int>(type: "INTEGER", nullable: true),
                    PrijsPerRijder = table.Column<decimal>(type: "TEXT", nullable: true),
                    IsGratis = table.Column<bool>(type: "INTEGER", nullable: false),
                    Categorieen = table.Column<string>(type: "TEXT", nullable: false),
                    Disciplines = table.Column<string>(type: "TEXT", nullable: false),
                    CoachesJson = table.Column<string>(type: "TEXT", nullable: false),
                    GepubliceerdAanbodId = table.Column<int>(type: "INTEGER", nullable: true),
                    BijgewerktOp = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingVoorstellen", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrainingVoorstellen");
        }
    }
}
