using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    public partial class PersoonlijkCoachen : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Make Datum nullable in RijderAanvragen
            migrationBuilder.AlterColumn<DateTime>(
                name: "Datum",
                table: "RijderAanvragen",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

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
            migrationBuilder.AlterColumn<DateTime>(
                name: "Datum",
                table: "RijderAanvragen",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldNullable: true,
                oldType: "TEXT");

            migrationBuilder.DropColumn(name: "IsGroep", table: "RijderAanvragen");
            migrationBuilder.DropColumn(name: "SoortOpdracht", table: "RijderAanvragen");
            migrationBuilder.DropColumn(name: "GroepGrootte", table: "RijderAanvragen");
            migrationBuilder.DropColumn(name: "WedstrijdNaam", table: "RijderAanvragen");
            migrationBuilder.DropColumn(name: "WedstrijdOrganisatie", table: "RijderAanvragen");
            migrationBuilder.DropColumn(name: "IsGroep", table: "CoachAanboden");
        }
    }
}
