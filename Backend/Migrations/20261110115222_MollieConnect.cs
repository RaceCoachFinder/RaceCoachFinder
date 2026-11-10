using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class MollieConnect : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MollieModus",
                table: "Boekingen",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Servicekosten",
                table: "Boekingen",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MollieKoppelingen",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GebruikerId = table.Column<string>(type: "TEXT", nullable: false),
                    OrganisatieNaam = table.Column<string>(type: "TEXT", nullable: true),
                    ProfielId = table.Column<string>(type: "TEXT", nullable: true),
                    RefreshTokenVersleuteld = table.Column<string>(type: "TEXT", nullable: false),
                    AccessTokenVersleuteld = table.Column<string>(type: "TEXT", nullable: true),
                    AccessTokenVerlooptOp = table.Column<DateTime>(type: "TEXT", nullable: true),
                    OnboardingStatus = table.Column<string>(type: "TEXT", nullable: true),
                    KanBetalingenOntvangen = table.Column<bool>(type: "INTEGER", nullable: false),
                    GekoppeldOp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    BijgewerktOp = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MollieKoppelingen", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MollieKoppelingen_GebruikerId",
                table: "MollieKoppelingen",
                column: "GebruikerId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MollieKoppelingen");

            migrationBuilder.DropColumn(
                name: "MollieModus",
                table: "Boekingen");

            migrationBuilder.DropColumn(
                name: "Servicekosten",
                table: "Boekingen");
        }
    }
}
