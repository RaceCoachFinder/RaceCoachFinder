using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    public partial class RijderUitnodigingen : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UitgenodigdeRijders",
                table: "RijderAanvragen",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<int>(
                name: "RijderAanvraagId",
                table: "Groepsgesprekken",
                type: "INTEGER",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "UitgenodigdeRijders", table: "RijderAanvragen");
            migrationBuilder.DropColumn(name: "RijderAanvraagId", table: "Groepsgesprekken");
        }
    }
}
