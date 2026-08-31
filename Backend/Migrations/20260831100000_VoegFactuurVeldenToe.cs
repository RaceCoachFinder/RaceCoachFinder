using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    public partial class VoegFactuurVeldenToe : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FactuurJson",
                table: "Boekingen",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FactuurnummerTekst",
                table: "Boekingen",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BetalingsTermijn",
                table: "Boekingen",
                type: "INTEGER",
                nullable: false,
                defaultValue: 14);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "FactuurJson", table: "Boekingen");
            migrationBuilder.DropColumn(name: "FactuurnummerTekst", table: "Boekingen");
            migrationBuilder.DropColumn(name: "BetalingsTermijn", table: "Boekingen");
        }
    }
}
