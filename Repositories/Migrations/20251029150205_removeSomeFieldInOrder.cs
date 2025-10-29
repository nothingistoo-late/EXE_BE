using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class removeSomeFieldInOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsWeeklyPackage",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ScheduledDeliveryDate",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "WeeklyPackageId",
                table: "Orders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsWeeklyPackage",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledDeliveryDate",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WeeklyPackageId",
                table: "Orders",
                type: "uniqueidentifier",
                nullable: true);
        }
    }
}
