using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class VoegPercentageToeAanUitnodiging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Percentage",
                table: "CoachAanbodUitnodigingen",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "BetalingsTermijn",
                table: "Boekingen",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldDefaultValue: 14);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Percentage",
                table: "CoachAanbodUitnodigingen");

            migrationBuilder.AlterColumn<int>(
                name: "BetalingsTermijn",
                table: "Boekingen",
                type: "INTEGER",
                nullable: false,
                defaultValue: 14,
                oldClrType: typeof(int),
                oldType: "INTEGER");
        }
    }
}
