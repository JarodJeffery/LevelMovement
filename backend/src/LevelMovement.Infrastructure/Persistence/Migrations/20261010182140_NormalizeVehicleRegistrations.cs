using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LevelMovement.Infrastructure.Persistence.Migrations
{
    public partial class NormalizeVehicleRegistrations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                -- Unicode whitespace recognised by char.IsWhiteSpace, including pasted non-breaking spaces.
                DECLARE @whitespace nvarchar(25) = NCHAR(9) + NCHAR(10) + NCHAR(11) + NCHAR(12) + NCHAR(13)
                    + NCHAR(32) + NCHAR(133) + NCHAR(160) + NCHAR(5760)
                    + NCHAR(8192) + NCHAR(8193) + NCHAR(8194) + NCHAR(8195) + NCHAR(8196) + NCHAR(8197)
                    + NCHAR(8198) + NCHAR(8199) + NCHAR(8200) + NCHAR(8201) + NCHAR(8202)
                    + NCHAR(8232) + NCHAR(8233) + NCHAR(8239) + NCHAR(8287) + NCHAR(12288);

                SELECT [Id], UPPER(REPLACE(TRANSLATE([RegistrationNumber] COLLATE Latin1_General_100_BIN2,
                    @whitespace, REPLICATE(N' ', 25)), N' ', N'')) AS [RegistrationNumber]
                INTO #NormalizedRegistrations
                FROM [Vehicles] WITH (TABLOCKX, HOLDLOCK);

                IF EXISTS (SELECT 1 FROM #NormalizedRegistrations WHERE [RegistrationNumber] = N'')
                    THROW 51000, 'Registration normalization found a blank plate. Correct it before retrying the migration.', 1;

                -- Include soft-deleted rows because the unique registration index includes them too.
                IF EXISTS (SELECT 1 FROM #NormalizedRegistrations
                    GROUP BY [RegistrationNumber] COLLATE DATABASE_DEFAULT HAVING COUNT(*) > 1)
                    THROW 51001, 'Registration normalization found duplicate plates after removing whitespace. Resolve the duplicates before retrying; no vehicles have been merged or deleted.', 1;

                UPDATE vehicle SET [RegistrationNumber] = normalized.[RegistrationNumber], [ConcurrencyStamp] = NEWID()
                FROM [Vehicles] AS vehicle
                INNER JOIN #NormalizedRegistrations AS normalized ON vehicle.[Id] = normalized.[Id]
                WHERE CONVERT(varbinary(40), vehicle.[RegistrationNumber]) <> CONVERT(varbinary(40), normalized.[RegistrationNumber]);

                DROP TABLE #NormalizedRegistrations;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
