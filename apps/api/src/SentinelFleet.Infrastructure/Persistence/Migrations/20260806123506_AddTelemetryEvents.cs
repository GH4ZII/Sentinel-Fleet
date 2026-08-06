using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SentinelFleet.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTelemetryEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "telemetry_events",
                schema: "sentinel",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    SpeedKph = table.Column<double>(type: "double precision", nullable: true),
                    Heading = table.Column<double>(type: "double precision", nullable: true),
                    IgnitionOn = table.Column<bool>(type: "boolean", nullable: true),
                    OdometerKm = table.Column<double>(type: "double precision", nullable: true),
                    FuelLevelPercent = table.Column<double>(type: "double precision", nullable: true),
                    DriverUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RawPayload = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_telemetry_events", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_telemetry_events_DeviceId",
                schema: "sentinel",
                table: "telemetry_events",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_telemetry_events_EventId",
                schema: "sentinel",
                table: "telemetry_events",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_telemetry_events_OrganizationId_AssetId_RecordedAt",
                schema: "sentinel",
                table: "telemetry_events",
                columns: new[] { "OrganizationId", "AssetId", "RecordedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "telemetry_events",
                schema: "sentinel");
        }
    }
}
