using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ayoos.Infrastructure.Persistence.Migrations.Ayoos;

/// <inheritdoc />
public partial class InitialAyoos : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Practices",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                Slug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                TimeZone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Address_Line1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Address_Line2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                Address_City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Address_State = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Address_PostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                Address_Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                ContactEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                ContactPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                TenantId = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Practices", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Practices_Slug",
            table: "Practices",
            column: "Slug",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Practices");
    }
}
