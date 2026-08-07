using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SentinelFleet.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIncidentsRiskAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_logs",
                schema: "sentinel",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    OldValues = table.Column<string>(type: "jsonb", nullable: true),
                    NewValues = table.Column<string>(type: "jsonb", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "incident_attachments",
                schema: "sentinel",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_incident_attachments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "incident_comments",
                schema: "sentinel",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_incident_comments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "incident_entities",
                schema: "sentinel",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    RelationshipType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FirstObservedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastObservedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_incident_entities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "incident_timeline_entries",
                schema: "sentinel",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntryType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    SourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_incident_timeline_entries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "incidents",
                schema: "sentinel",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrimaryAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    IncidentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RiskScore = table.Column<int>(type: "integer", nullable: false),
                    Confidence = table.Column<double>(type: "double precision", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DetectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AssignedToUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_incidents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "risk_assessments",
                schema: "sentinel",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    RiskLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Factors = table.Column<string>(type: "jsonb", nullable: false),
                    ModelVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CalculatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_risk_assessments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_detections_IncidentId",
                schema: "sentinel",
                table: "detections",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_EntityType_EntityId",
                schema: "sentinel",
                table: "audit_logs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_OrganizationId",
                schema: "sentinel",
                table: "audit_logs",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_OrganizationId_CreatedAt",
                schema: "sentinel",
                table: "audit_logs",
                columns: new[] { "OrganizationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_incident_attachments_IncidentId",
                schema: "sentinel",
                table: "incident_attachments",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_incident_attachments_OrganizationId",
                schema: "sentinel",
                table: "incident_attachments",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_incident_attachments_StorageKey",
                schema: "sentinel",
                table: "incident_attachments",
                column: "StorageKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_incident_comments_IncidentId",
                schema: "sentinel",
                table: "incident_comments",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_incident_comments_OrganizationId",
                schema: "sentinel",
                table: "incident_comments",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_incident_entities_IncidentId",
                schema: "sentinel",
                table: "incident_entities",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_incident_entities_IncidentId_EntityType_EntityId",
                schema: "sentinel",
                table: "incident_entities",
                columns: new[] { "IncidentId", "EntityType", "EntityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_incident_entities_OrganizationId",
                schema: "sentinel",
                table: "incident_entities",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_incident_timeline_entries_IncidentId",
                schema: "sentinel",
                table: "incident_timeline_entries",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_incident_timeline_entries_IncidentId_Timestamp",
                schema: "sentinel",
                table: "incident_timeline_entries",
                columns: new[] { "IncidentId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_incident_timeline_entries_OrganizationId",
                schema: "sentinel",
                table: "incident_timeline_entries",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_incidents_OrganizationId",
                schema: "sentinel",
                table: "incidents",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_incidents_OrganizationId_DetectedAt",
                schema: "sentinel",
                table: "incidents",
                columns: new[] { "OrganizationId", "DetectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_incidents_OrganizationId_PrimaryAssetId_Status",
                schema: "sentinel",
                table: "incidents",
                columns: new[] { "OrganizationId", "PrimaryAssetId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_incidents_OrganizationId_Status",
                schema: "sentinel",
                table: "incidents",
                columns: new[] { "OrganizationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_risk_assessments_IncidentId",
                schema: "sentinel",
                table: "risk_assessments",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_risk_assessments_IncidentId_CalculatedAt",
                schema: "sentinel",
                table: "risk_assessments",
                columns: new[] { "IncidentId", "CalculatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_risk_assessments_OrganizationId",
                schema: "sentinel",
                table: "risk_assessments",
                column: "OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_detections_incidents_IncidentId",
                schema: "sentinel",
                table: "detections",
                column: "IncidentId",
                principalSchema: "sentinel",
                principalTable: "incidents",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_detections_incidents_IncidentId",
                schema: "sentinel",
                table: "detections");

            migrationBuilder.DropTable(
                name: "audit_logs",
                schema: "sentinel");

            migrationBuilder.DropTable(
                name: "incident_attachments",
                schema: "sentinel");

            migrationBuilder.DropTable(
                name: "incident_comments",
                schema: "sentinel");

            migrationBuilder.DropTable(
                name: "incident_entities",
                schema: "sentinel");

            migrationBuilder.DropTable(
                name: "incident_timeline_entries",
                schema: "sentinel");

            migrationBuilder.DropTable(
                name: "incidents",
                schema: "sentinel");

            migrationBuilder.DropTable(
                name: "risk_assessments",
                schema: "sentinel");

            migrationBuilder.DropIndex(
                name: "IX_detections_IncidentId",
                schema: "sentinel",
                table: "detections");
        }
    }
}
