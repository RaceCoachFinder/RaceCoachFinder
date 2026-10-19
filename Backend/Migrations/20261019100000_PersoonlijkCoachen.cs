using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    public partial class PersoonlijkCoachen : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQLite cannot ALTER COLUMN directly — rebuild the table via raw SQL.
            // Only original columns here; AddColumn below adds the new ones.
            migrationBuilder.Sql("""
                CREATE TABLE "RijderAanvragen_new" (
                    "Id"            INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    "AangemaaktOp"  TEXT    NOT NULL,
                    "Beschrijving"  TEXT    NOT NULL,
                    "CoachGevonden" INTEGER NOT NULL,
                    "Datum"         TEXT,
                    "IsGesloten"    INTEGER NOT NULL,
                    "Kwaliteiten"   TEXT    NOT NULL,
                    "Locatie"       TEXT    NOT NULL,
                    "MaxPrijs"      TEXT,
                    "RijderId"      TEXT    NOT NULL,
                    "RijderNaam"    TEXT    NOT NULL,
                    "Specialisaties" TEXT   NOT NULL
                );
                INSERT INTO "RijderAanvragen_new"
                    ("Id","AangemaaktOp","Beschrijving","CoachGevonden","Datum",
                     "IsGesloten","Kwaliteiten","Locatie","MaxPrijs","RijderId",
                     "RijderNaam","Specialisaties")
                SELECT "Id","AangemaaktOp","Beschrijving","CoachGevonden","Datum",
                       "IsGesloten","Kwaliteiten","Locatie","MaxPrijs","RijderId",
                       "RijderNaam","Specialisaties"
                FROM   "RijderAanvragen";
                DROP TABLE "RijderAanvragen";
                ALTER TABLE "RijderAanvragen_new" RENAME TO "RijderAanvragen";
                """);

            migrationBuilder.AddColumn<bool>(
                name: "IsGroep",
                table: "RijderAanvragen",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SoortOpdracht",
                table: "RijderAanvragen",
                type: "TEXT",
                nullable: false,
                defaultValue: "Training");

            migrationBuilder.AddColumn<int>(
                name: "GroepGrootte",
                table: "RijderAanvragen",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WedstrijdNaam",
                table: "RijderAanvragen",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WedstrijdOrganisatie",
                table: "RijderAanvragen",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsGroep",
                table: "CoachAanboden",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "IsGroep", table: "RijderAanvragen");
            migrationBuilder.DropColumn(name: "SoortOpdracht", table: "RijderAanvragen");
            migrationBuilder.DropColumn(name: "GroepGrootte", table: "RijderAanvragen");
            migrationBuilder.DropColumn(name: "WedstrijdNaam", table: "RijderAanvragen");
            migrationBuilder.DropColumn(name: "WedstrijdOrganisatie", table: "RijderAanvragen");
            migrationBuilder.DropColumn(name: "IsGroep", table: "CoachAanboden");

            migrationBuilder.Sql("""
                CREATE TABLE "RijderAanvragen_old" (
                    "Id"            INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    "AangemaaktOp"  TEXT    NOT NULL,
                    "Beschrijving"  TEXT    NOT NULL,
                    "CoachGevonden" INTEGER NOT NULL,
                    "Datum"         TEXT    NOT NULL,
                    "IsGesloten"    INTEGER NOT NULL,
                    "Kwaliteiten"   TEXT    NOT NULL,
                    "Locatie"       TEXT    NOT NULL,
                    "MaxPrijs"      TEXT,
                    "RijderId"      TEXT    NOT NULL,
                    "RijderNaam"    TEXT    NOT NULL,
                    "Specialisaties" TEXT   NOT NULL
                );
                INSERT INTO "RijderAanvragen_old"
                    ("Id","AangemaaktOp","Beschrijving","CoachGevonden","Datum",
                     "IsGesloten","Kwaliteiten","Locatie","MaxPrijs","RijderId",
                     "RijderNaam","Specialisaties")
                SELECT "Id","AangemaaktOp","Beschrijving","CoachGevonden","Datum",
                       "IsGesloten","Kwaliteiten","Locatie","MaxPrijs","RijderId",
                       "RijderNaam","Specialisaties"
                FROM   "RijderAanvragen";
                DROP TABLE "RijderAanvragen";
                ALTER TABLE "RijderAanvragen_old" RENAME TO "RijderAanvragen";
                """);
        }
    }
}
