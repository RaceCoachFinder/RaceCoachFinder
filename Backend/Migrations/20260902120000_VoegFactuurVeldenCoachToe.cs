using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    public partial class VoegFactuurVeldenCoachToe : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "FactuurAdres", table: "Coaches", type: "TEXT", nullable: true);
            migrationBuilder.AddColumn<string>(name: "FactuurPostcode", table: "Coaches", type: "TEXT", nullable: true);
            migrationBuilder.AddColumn<string>(name: "FactuurStad", table: "Coaches", type: "TEXT", nullable: true);
            migrationBuilder.AddColumn<string>(name: "FactuurLand", table: "Coaches", type: "TEXT", nullable: true);
            migrationBuilder.AddColumn<string>(name: "FactuurTelefoon", table: "Coaches", type: "TEXT", nullable: true);
            migrationBuilder.AddColumn<string>(name: "KvkNummer", table: "Coaches", type: "TEXT", nullable: true);
            migrationBuilder.AddColumn<string>(name: "BtwNummer", table: "Coaches", type: "TEXT", nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "FactuurAdres", table: "Coaches");
            migrationBuilder.DropColumn(name: "FactuurPostcode", table: "Coaches");
            migrationBuilder.DropColumn(name: "FactuurStad", table: "Coaches");
            migrationBuilder.DropColumn(name: "FactuurLand", table: "Coaches");
            migrationBuilder.DropColumn(name: "FactuurTelefoon", table: "Coaches");
            migrationBuilder.DropColumn(name: "KvkNummer", table: "Coaches");
            migrationBuilder.DropColumn(name: "BtwNummer", table: "Coaches");
        }
    }
}
