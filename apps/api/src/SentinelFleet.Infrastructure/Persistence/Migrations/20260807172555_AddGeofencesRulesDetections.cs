using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace SentinelFleet.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGeofencesRulesDetections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "asset_presences",
                schema: "sentinel",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    GeofenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsInside = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asset_presences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "detection_rules",
                schema: "sentinel",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RuleType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Configuration = table.Column<string>(type: "jsonb", nullable: true),
                    Severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_detection_rules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "detections",
                schema: "sentinel",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleId = table.Column<Guid>(type: "uuid", nullable: true),
                    DetectionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Confidence = table.Column<double>(type: "double precision", nullable: false),
                    RiskContribution = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    TriggeredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SourceEventIds = table.Column<string>(type: "jsonb", nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_detections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "driver_assignments",
                schema: "sentinel",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ValidFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ValidTo = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AssignmentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_driver_assignments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "geofences",
                schema: "sentinel",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Geometry = table.Column<Polygon>(type: "geometry(Polygon, 4326)", nullable: false),
                    GeofenceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_geofences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "work_shifts",
                schema: "sentinel",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_shifts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "asset_geofences",
                schema: "sentinel",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    GeofenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ValidFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ValidTo = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asset_geofences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_asset_geofences_geofences_GeofenceId",
                        column: x => x.GeofenceId,
                        principalSchema: "sentinel",
                        principalTable: "geofences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_asset_geofences_AssetId_GeofenceId",
                schema: "sentinel",
                table: "asset_geofences",
                columns: new[] { "AssetId", "GeofenceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_asset_geofences_GeofenceId",
                schema: "sentinel",
                table: "asset_geofences",
                column: "GeofenceId");

            migrationBuilder.CreateIndex(
                name: "IX_asset_geofences_OrganizationId",
                schema: "sentinel",
                table: "asset_geofences",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_asset_presences_AssetId_GeofenceId",
                schema: "sentinel",
                table: "asset_presences",
                columns: new[] { "AssetId", "GeofenceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_asset_presences_OrganizationId",
                schema: "sentinel",
                table: "asset_presences",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_detection_rules_OrganizationId",
                schema: "sentinel",
                table: "detection_rules",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_detection_rules_OrganizationId_RuleType",
                schema: "sentinel",
                table: "detection_rules",
                columns: new[] { "OrganizationId", "RuleType" });

            migrationBuilder.CreateIndex(
                name: "IX_detections_AssetId_DetectionType_TriggeredAt",
                schema: "sentinel",
                table: "detections",
                columns: new[] { "AssetId", "DetectionType", "TriggeredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_detections_OrganizationId",
                schema: "sentinel",
                table: "detections",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_detections_OrganizationId_TriggeredAt",
                schema: "sentinel",
                table: "detections",
                columns: new[] { "OrganizationId", "TriggeredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_driver_assignments_AssetId_UserId_ValidFrom",
                schema: "sentinel",
                table: "driver_assignments",
                columns: new[] { "AssetId", "UserId", "ValidFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_driver_assignments_OrganizationId",
                schema: "sentinel",
                table: "driver_assignments",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_geofences_Geometry",
                schema: "sentinel",
                table: "geofences",
                column: "Geometry")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_geofences_OrganizationId",
                schema: "sentinel",
                table: "geofences",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_work_shifts_OrganizationId",
                schema: "sentinel",
                table: "work_shifts",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_work_shifts_UserId_StartsAt_EndsAt",
                schema: "sentinel",
                table: "work_shifts",
                columns: new[] { "UserId", "StartsAt", "EndsAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "asset_geofences",
                schema: "sentinel");

            migrationBuilder.DropTable(
                name: "asset_presences",
                schema: "sentinel");

            migrationBuilder.DropTable(
                name: "detection_rules",
                schema: "sentinel");

            migrationBuilder.DropTable(
                name: "detections",
                schema: "sentinel");

            migrationBuilder.DropTable(
                name: "driver_assignments",
                schema: "sentinel");

            migrationBuilder.DropTable(
                name: "work_shifts",
                schema: "sentinel");

            migrationBuilder.DropTable(
                name: "geofences",
                schema: "sentinel");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:postgis", ",,");
        }
    }
}
