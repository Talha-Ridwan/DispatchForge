using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddEventsAndDeliveryOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            /*
             * Hand-edited: EF can't express PARTITION BY, so Events is created with raw SQL.
             * Must run first: the outbox FK and the unique index below both need Events to exist.
             * Partitions are NOT created here; the partition maintenance service creates
             * this week + the next few on startup (dates relative to now, not migration time).
             */
            migrationBuilder.Sql("""
                CREATE TABLE "Events" (
                    "Id"              uuid        NOT NULL,
                    "ReceivedAt"      timestamptz NOT NULL,
                    "TenantId"        uuid        NOT NULL,
                    "EventTypeId"     uuid        NOT NULL,
                    "IdempotencyKey"  text        NOT NULL,
                    "IdempotencyHash" bytea       NOT NULL,
                    "Payload"         bytea       NOT NULL,
                    CONSTRAINT "PK_Events" PRIMARY KEY ("Id", "ReceivedAt")
                ) PARTITION BY RANGE ("ReceivedAt");
                """);

            migrationBuilder.CreateTable(
                name: "DeliveryOutboxes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DestinationId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LeasedBy = table.Column<string>(type: "text", nullable: true),
                    LeaseExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryOutboxes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryOutboxes_Destinations_DestinationId",
                        column: x => x.DestinationId,
                        principalTable: "Destinations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeliveryOutboxes_Events_EventId_EventReceivedAt",
                        columns: x => new { x.EventId, x.EventReceivedAt },
                        principalTable: "Events",
                        principalColumns: new[] { "Id", "ReceivedAt" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryOutboxes_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryOutboxes_DestinationId",
                table: "DeliveryOutboxes",
                column: "DestinationId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryOutboxes_EventId_EventReceivedAt",
                table: "DeliveryOutboxes",
                columns: new[] { "EventId", "EventReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryOutboxes_ScheduledAt",
                table: "DeliveryOutboxes",
                column: "ScheduledAt");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryOutboxes_TenantId",
                table: "DeliveryOutboxes",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_TenantId_IdempotencyHash_ReceivedAt",
                table: "Events",
                columns: new[] { "TenantId", "IdempotencyHash", "ReceivedAt" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeliveryOutboxes");

            migrationBuilder.DropTable(
                name: "Events");
        }
    }
}
