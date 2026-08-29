using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    public partial class VoegAbonnementVeldenToe : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AbonnementActief",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "AbonnementVerlooptOp",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MollieKlantId",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "AbonnementActief", table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "AbonnementVerlooptOp", table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "MollieKlantId", table: "AspNetUsers");
        }
    }
}
