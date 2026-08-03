using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ayoos.Infrastructure.Persistence.Migrations.Ayoos
{
    /// <inheritdoc />
    public partial class AddBookings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bookings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    AvailabilityScheduleId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.Id);
                    table.CheckConstraint("CK_Bookings_StartBeforeEnd", "\"StartTime\" < \"EndTime\"");
                    table.ForeignKey(
                        name: "FK_Bookings_AvailabilitySchedules_AvailabilityScheduleId",
                        column: x => x.AvailabilityScheduleId,
                        principalTable: "AvailabilitySchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Bookings_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bookings_Providers_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_AvailabilityScheduleId",
                table: "Bookings",
                column: "AvailabilityScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_PatientId",
                table: "Bookings",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ProviderId",
                table: "Bookings",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_TenantId_PatientId_StartTime",
                table: "Bookings",
                columns: new[] { "TenantId", "PatientId", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_TenantId_ProviderId_StartTime_EndTime_Status",
                table: "Bookings",
                columns: new[] { "TenantId", "ProviderId", "StartTime", "EndTime", "Status" });

            migrationBuilder.Sql(
                """
                CREATE EXTENSION IF NOT EXISTS btree_gist;
                ALTER TABLE "Bookings"
                    ADD CONSTRAINT "EX_Bookings_NoProviderOverlap"
                    EXCLUDE USING gist (
                        "TenantId" WITH =,
                        "ProviderId" WITH =,
                        tstzrange("StartTime", "EndTime", '[)') WITH &&
                    )
                    WHERE ("Status" <> 'Cancelled');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bookings");
        }
    }
}
