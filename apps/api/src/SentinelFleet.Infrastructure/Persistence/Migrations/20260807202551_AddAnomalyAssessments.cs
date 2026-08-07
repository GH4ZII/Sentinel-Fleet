using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SentinelFleet.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAnomalyAssessments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "anomaly_assessments",
                schema: "sentinel",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    TelemetryEventId = table.Column<Guid>(type: "uuid", nullable: true),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Score = table.Column<double>(type: "double precision", nullable: false),
                    Confidence = table.Column<double>(type: "double precision", nullable: false),
                    ModelVersion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FeaturesUsed = table.Column<string>(type: "jsonb", nullable: true),
                    Explanation = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    IsAnomaly = table.Column<bool>(type: "boolean", nullable: false),
                    CalculatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_anomaly_assessments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_anomaly_assessments_AssetId_CalculatedAt",
                schema: "sentinel",
                table: "anomaly_assessments",
                columns: new[] { "AssetId", "CalculatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_anomaly_assessments_IncidentId",
                schema: "sentinel",
                table: "anomaly_assessments",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_anomaly_assessments_IsAnomaly",
                schema: "sentinel",
                table: "anomaly_assessments",
                column: "IsAnomaly");

            migrationBuilder.CreateIndex(
                name: "IX_anomaly_assessments_OrganizationId",
                schema: "sentinel",
                table: "anomaly_assessments",
                column: "OrganizationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "anomaly_assessments",
                schema: "sentinel");
        }
    }
}
