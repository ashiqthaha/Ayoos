using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ayoos.Infrastructure.Persistence.Migrations.Ayoos
{
    /// <inheritdoc />
    public partial class AddProviderAvailability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AvailabilityExceptions_ProviderId_Date",
                table: "AvailabilityExceptions");

            migrationBuilder.DropIndex(
                name: "IX_AvailabilityRules_ProviderId_DayOfWeek",
                table: "AvailabilityRules");

            migrationBuilder.RenameTable(
                name: "AvailabilityRules",
                newName: "AvailabilitySchedules");

            migrationBuilder.DropColumn(
                name: "EffectiveFrom",
                table: "AvailabilitySchedules");

            migrationBuilder.DropColumn(
                name: "EffectiveTo",
                table: "AvailabilitySchedules");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "AvailabilitySchedules",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilityExceptions_ProviderId",
                table: "AvailabilityExceptions",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilityExceptions_TenantId_ProviderId_Date",
                table: "AvailabilityExceptions",
                columns: new[] { "TenantId", "ProviderId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilitySchedules_ProviderId",
                table: "AvailabilitySchedules",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilitySchedules_TenantId_ProviderId_DayOfWeek_IsActive",
                table: "AvailabilitySchedules",
                columns: new[] { "TenantId", "ProviderId", "DayOfWeek", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AvailabilityExceptions_ProviderId",
                table: "AvailabilityExceptions");

            migrationBuilder.DropIndex(
                name: "IX_AvailabilityExceptions_TenantId_ProviderId_Date",
                table: "AvailabilityExceptions");

            migrationBuilder.DropIndex(
                name: "IX_AvailabilitySchedules_ProviderId",
                table: "AvailabilitySchedules");

            migrationBuilder.DropIndex(
                name: "IX_AvailabilitySchedules_TenantId_ProviderId_DayOfWeek_IsActive",
                table: "AvailabilitySchedules");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "AvailabilitySchedules");

            migrationBuilder.AddColumn<DateOnly>(
                name: "EffectiveFrom",
                table: "AvailabilitySchedules",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1970, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "EffectiveTo",
                table: "AvailabilitySchedules",
                type: "date",
                nullable: true);

            migrationBuilder.RenameTable(
                name: "AvailabilitySchedules",
                newName: "AvailabilityRules");

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilityExceptions_ProviderId_Date",
                table: "AvailabilityExceptions",
                columns: new[] { "ProviderId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilityRules_ProviderId_DayOfWeek",
                table: "AvailabilityRules",
                columns: new[] { "ProviderId", "DayOfWeek" });
        }
    }
}
