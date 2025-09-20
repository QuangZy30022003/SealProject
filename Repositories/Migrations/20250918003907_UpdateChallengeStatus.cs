using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class UpdateChallengeStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "Challenges");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Challenges",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Challenges");

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "Challenges",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
