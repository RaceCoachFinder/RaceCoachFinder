using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    public partial class VoegCoachAanbodToe : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CoachAanboden",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CoachGebruikerId = table.Column<string>(type: "TEXT", nullable: false),
                    CoachNaam = table.Column<string>(type: "TEXT", nullable: false),
                    Titel = table.Column<string>(type: "TEXT", nullable: false),
                    Beschrijving = table.Column<string>(type: "TEXT", nullable: false),
                    Datum = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Locatie = table.Column<string>(type: "TEXT", nullable: false),
                    Categorieen = table.Column<string>(type: "TEXT", nullable: false),
                    Disciplines = table.Column<string>(type: "TEXT", nullable: false),
                    MaxRijders = table.Column<int>(type: "INTEGER", nullable: true),
                    PrijsPerRijder = table.Column<decimal>(type: "TEXT", nullable: true),
                    IsGratis = table.Column<bool>(type: "INTEGER", nullable: false),
                    AangemaaktOp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActief = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachAanboden", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CoachAanboden");
        }
    }
}
