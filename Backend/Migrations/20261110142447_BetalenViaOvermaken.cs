using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class BetalenViaOvermaken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Iban",
                table: "Coaches",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IbanTenaamstelling",
                table: "Coaches",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BetaalLink",
                table: "Boekingen",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BetaalWijze",
                table: "Boekingen",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Iban",
                table: "Boekingen",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IbanTenaamstelling",
                table: "Boekingen",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RijderMeldtBetaaldOp",
                table: "Boekingen",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Iban",
                table: "Coaches");

            migrationBuilder.DropColumn(
                name: "IbanTenaamstelling",
                table: "Coaches");

            migrationBuilder.DropColumn(
                name: "BetaalLink",
                table: "Boekingen");

            migrationBuilder.DropColumn(
                name: "BetaalWijze",
                table: "Boekingen");

            migrationBuilder.DropColumn(
                name: "Iban",
                table: "Boekingen");

            migrationBuilder.DropColumn(
                name: "IbanTenaamstelling",
                table: "Boekingen");

            migrationBuilder.DropColumn(
                name: "RijderMeldtBetaaldOp",
                table: "Boekingen");
        }
    }
}
