using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class FilterEventTypeNameIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EventTypes_TenantId_Name",
                table: "EventTypes");

            migrationBuilder.CreateIndex(
                name: "IX_EventTypes_TenantId_Name",
                table: "EventTypes",
                columns: new[] { "TenantId", "Name" },
                unique: true,
                filter: "\"Status\" <> 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EventTypes_TenantId_Name",
                table: "EventTypes");

            migrationBuilder.CreateIndex(
                name: "IX_EventTypes_TenantId_Name",
                table: "EventTypes",
                columns: new[] { "TenantId", "Name" },
                unique: true);
        }
    }
}
