using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    public partial class VoegAgendaReminderToe : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AgendaReminderActief",
                table: "GebruikerInstellingen",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "AgendaReminderDagenVanTevoren",
                table: "GebruikerInstellingen",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "LaatsteReminderDatum",
                table: "GebruikerInstellingen",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "AgendaReminderActief",          table: "GebruikerInstellingen");
            migrationBuilder.DropColumn(name: "AgendaReminderDagenVanTevoren", table: "GebruikerInstellingen");
            migrationBuilder.DropColumn(name: "LaatsteReminderDatum",          table: "GebruikerInstellingen");
        }
    }
}
