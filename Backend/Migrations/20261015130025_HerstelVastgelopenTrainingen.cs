using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class HerstelVastgelopenTrainingen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Eenmalig herstel: trainingen waarbij alle coaches al geaccepteerd hebben,
            // maar die door een fout bij accepteren offline bleven staan
            migrationBuilder.Sql(@"
                UPDATE CoachAanboden
                SET IsActief = 1, WachtOpCoaches = 0
                WHERE WachtOpCoaches = 1
                  AND EXISTS (SELECT 1 FROM CoachAanbodUitnodigingen u WHERE u.CoachAanbodId = CoachAanboden.Id)
                  AND NOT EXISTS (SELECT 1 FROM CoachAanbodUitnodigingen u
                                  WHERE u.CoachAanbodId = CoachAanboden.Id AND u.Status <> 'Geaccepteerd');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
