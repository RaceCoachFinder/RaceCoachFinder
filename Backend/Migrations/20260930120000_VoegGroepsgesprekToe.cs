using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    public partial class VoegGroepsgesprekToe : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CoachAanbodUitnodigingen",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CoachAanbodId = table.Column<int>(type: "INTEGER", nullable: false),
                    AanbodTitel = table.Column<string>(type: "TEXT", nullable: false),
                    RijderGebruikerId = table.Column<string>(type: "TEXT", nullable: false),
                    RijderNaam = table.Column<string>(type: "TEXT", nullable: false),
                    CoachGebruikerId = table.Column<string>(type: "TEXT", nullable: false),
                    CoachNaam = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "Openstaand"),
                    AangemaaktOp = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachAanbodUitnodigingen", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Groepsgesprekken",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CoachAanbodId = table.Column<int>(type: "INTEGER", nullable: false),
                    Naam = table.Column<string>(type: "TEXT", nullable: false),
                    AangemaaktDoorId = table.Column<string>(type: "TEXT", nullable: false),
                    AangemaaktOp = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groepsgesprekken", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GroepsgesprekLeden",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GroepsgesprekId = table.Column<int>(type: "INTEGER", nullable: false),
                    GebruikerId = table.Column<string>(type: "TEXT", nullable: false),
                    GebruikerNaam = table.Column<string>(type: "TEXT", nullable: false),
                    ToegetreedOp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LaatstGelezen = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroepsgesprekLeden", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Groepsberichten",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GroepsgesprekId = table.Column<int>(type: "INTEGER", nullable: false),
                    VanGebruikerId = table.Column<string>(type: "TEXT", nullable: false),
                    VanNaam = table.Column<string>(type: "TEXT", nullable: false),
                    Tekst = table.Column<string>(type: "TEXT", nullable: false),
                    AangemaaktOp = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groepsberichten", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CoachAanbodUitnodigingen");
            migrationBuilder.DropTable(name: "Groepsgesprekken");
            migrationBuilder.DropTable(name: "GroepsgesprekLeden");
            migrationBuilder.DropTable(name: "Groepsberichten");
        }
    }
}
