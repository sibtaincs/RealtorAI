using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RealEstateAI.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class DealerApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ApprovalStatus",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE [AspNetUsers] SET [ApprovalStatus] = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "AspNetUsers");
        }
    }
}
