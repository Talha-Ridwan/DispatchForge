using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class CryptographyKeysToBytea : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Keys were stored as base64 text; decode them into the raw 32 bytes. Empty secondary becomes NULL.
            migrationBuilder.Sql(
                """
                ALTER TABLE "Destinations"
                    ALTER COLUMN "CryptographyKeyPrimary" TYPE bytea USING decode("CryptographyKeyPrimary", 'base64'),
                    ALTER COLUMN "CryptographyKeySecondary" TYPE bytea USING decode(NULLIF("CryptographyKeySecondary", ''), 'base64');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "Destinations"
                    ALTER COLUMN "CryptographyKeyPrimary" TYPE text USING encode("CryptographyKeyPrimary", 'base64'),
                    ALTER COLUMN "CryptographyKeySecondary" TYPE text USING encode("CryptographyKeySecondary", 'base64');
                """);
        }
    }
}
