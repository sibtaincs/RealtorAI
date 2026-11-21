using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RealEstateAI.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class PropertyDetailsExpansion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AmenitiesJson",
                table: "Properties",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BuiltInYear",
                table: "Properties",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "Properties",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactLandline",
                table: "Properties",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactMobile",
                table: "Properties",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactWhatsapp",
                table: "Properties",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ElectricityBackup",
                table: "Properties",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Flooring",
                table: "Properties",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Floors",
                table: "Properties",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "InstallmentAvailable",
                table: "Properties",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsReadyForPossession",
                table: "Properties",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ParkingSpaces",
                table: "Properties",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlotNumber",
                table: "Properties",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "View",
                table: "Properties",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmenitiesJson",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "BuiltInYear",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "ContactLandline",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "ContactMobile",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "ContactWhatsapp",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "ElectricityBackup",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "Flooring",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "Floors",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "InstallmentAvailable",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "IsReadyForPossession",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "ParkingSpaces",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "PlotNumber",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "View",
                table: "Properties");
        }
    }
}
