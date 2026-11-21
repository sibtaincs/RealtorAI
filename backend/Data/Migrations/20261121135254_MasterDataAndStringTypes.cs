using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RealEstateAI.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class MasterDataAndStringTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Properties",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "ListingType",
                table: "Properties",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.Sql(@"
                UPDATE [Properties]
                SET [Type] = CASE [Type]
                        WHEN '0' THEN 'House' WHEN '1' THEN 'Plot' WHEN '2' THEN 'Apartment'
                        WHEN '3' THEN 'Shop' WHEN '4' THEN 'Office' WHEN '5' THEN 'Villa'
                        WHEN '6' THEN 'Farmhouse' ELSE [Type] END,
                    [ListingType] = CASE [ListingType]
                        WHEN '0' THEN 'Sale' WHEN '1' THEN 'Rent' ELSE [ListingType] END
                WHERE [Type] LIKE '[0-6]' OR [ListingType] LIKE '[0-1]';");

            migrationBuilder.CreateTable(
                name: "MasterData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kind = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterData", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MasterData_Kind_Value",
                table: "MasterData",
                columns: new[] { "Kind", "Value" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MasterData");

            migrationBuilder.Sql(@"
                UPDATE [Properties]
                SET [Type] = CASE [Type]
                        WHEN 'House' THEN '0' WHEN 'Plot' THEN '1' WHEN 'Apartment' THEN '2'
                        WHEN 'Shop' THEN '3' WHEN 'Office' THEN '4' WHEN 'Villa' THEN '5'
                        WHEN 'Farmhouse' THEN '6' ELSE [Type] END,
                    [ListingType] = CASE [ListingType]
                        WHEN 'Sale' THEN '0' WHEN 'Rent' THEN '1' ELSE [ListingType] END
                WHERE [Type] IN ('House','Plot','Apartment','Shop','Office','Villa','Farmhouse')
                   OR [ListingType] IN ('Sale','Rent');");

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "Properties",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<int>(
                name: "ListingType",
                table: "Properties",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);
        }
    }
}
