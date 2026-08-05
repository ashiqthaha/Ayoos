using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ayoos.Infrastructure.Persistence.Migrations.Ayoos
{
    /// <inheritdoc />
    public partial class AlignProviderAvailabilityP15 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OverrideStartTime",
                table: "AvailabilityExceptions",
                newName: "StartTime");

            migrationBuilder.RenameColumn(
                name: "OverrideEndTime",
                table: "AvailabilityExceptions",
                newName: "EndTime");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAtUtc",
                table: "AvailabilitySchedules",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAtUtc",
                table: "AvailabilitySchedules",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAtUtc",
                table: "AvailabilityExceptions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<int>(
                name: "ExceptionType",
                table: "AvailabilityExceptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                "UPDATE \"AvailabilityExceptions\" SET \"ExceptionType\" = CASE WHEN \"IsUnavailable\" THEN 0 ELSE 1 END;");

            migrationBuilder.DropColumn(
                name: "IsUnavailable",
                table: "AvailabilityExceptions");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAtUtc",
                table: "AvailabilityExceptions",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "AvailabilitySchedules");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "AvailabilitySchedules");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "AvailabilityExceptions");

            migrationBuilder.DropColumn(
                name: "ExceptionType",
                table: "AvailabilityExceptions");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "AvailabilityExceptions");

            migrationBuilder.RenameColumn(
                name: "StartTime",
                table: "AvailabilityExceptions",
                newName: "OverrideStartTime");

            migrationBuilder.RenameColumn(
                name: "EndTime",
                table: "AvailabilityExceptions",
                newName: "OverrideEndTime");

            migrationBuilder.AddColumn<bool>(
                name: "IsUnavailable",
                table: "AvailabilityExceptions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
