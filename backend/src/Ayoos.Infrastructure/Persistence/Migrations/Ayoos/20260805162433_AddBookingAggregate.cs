using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ayoos.Infrastructure.Persistence.Migrations.Ayoos
{
    /// <inheritdoc />
    public partial class AddBookingAggregate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE \"Bookings\" DROP CONSTRAINT IF EXISTS \"EX_Bookings_NoProviderOverlap\";");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_TenantId_PatientId_StartTime",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_TenantId_ProviderId_StartTime_EndTime_Status",
                table: "Bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Bookings_StartBeforeEnd",
                table: "Bookings");

            migrationBuilder.RenameColumn(
                name: "StartTime",
                table: "Bookings",
                newName: "ScheduledStart");

            migrationBuilder.RenameColumn(
                name: "EndTime",
                table: "Bookings",
                newName: "ScheduledEnd");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Bookings",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "Bookings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "Bookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "Bookings"
                SET "UpdatedAt" = "CreatedAt",
                    "Status" = CASE
                        WHEN "Status" = 'Requested' THEN 'Pending'
                        WHEN "Status" = 'Cancelled' THEN 'CancelledByProvider'
                        ELSE "Status"
                    END;
                """);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "Bookings",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_TenantId_PatientId_ScheduledStart",
                table: "Bookings",
                columns: new[] { "TenantId", "PatientId", "ScheduledStart" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_TenantId_ProviderId_ScheduledStart",
                table: "Bookings",
                columns: new[] { "TenantId", "ProviderId", "ScheduledStart" },
                unique: true,
                filter: "\"Status\" IN ('Pending', 'Confirmed')");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_TenantId_ProviderId_ScheduledStart_ScheduledEnd_St~",
                table: "Bookings",
                columns: new[] { "TenantId", "ProviderId", "ScheduledStart", "ScheduledEnd", "Status" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Bookings_ScheduledStartBeforeEnd",
                table: "Bookings",
                sql: "\"ScheduledStart\" < \"ScheduledEnd\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_TenantId_PatientId_ScheduledStart",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_TenantId_ProviderId_ScheduledStart",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_TenantId_ProviderId_ScheduledStart_ScheduledEnd_St~",
                table: "Bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Bookings_ScheduledStartBeforeEnd",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Bookings");

            migrationBuilder.RenameColumn(
                name: "ScheduledStart",
                table: "Bookings",
                newName: "StartTime");

            migrationBuilder.RenameColumn(
                name: "ScheduledEnd",
                table: "Bookings",
                newName: "EndTime");

            migrationBuilder.Sql(
                """
                UPDATE "Bookings"
                SET "Status" = CASE
                    WHEN "Status" = 'Pending' THEN 'Requested'
                    WHEN "Status" IN ('CancelledByPatient', 'CancelledByProvider') THEN 'Cancelled'
                    ELSE "Status"
                END;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Bookings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_TenantId_PatientId_StartTime",
                table: "Bookings",
                columns: new[] { "TenantId", "PatientId", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_TenantId_ProviderId_StartTime_EndTime_Status",
                table: "Bookings",
                columns: new[] { "TenantId", "ProviderId", "StartTime", "EndTime", "Status" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Bookings_StartBeforeEnd",
                table: "Bookings",
                sql: "\"StartTime\" < \"EndTime\"");

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
    }
}
