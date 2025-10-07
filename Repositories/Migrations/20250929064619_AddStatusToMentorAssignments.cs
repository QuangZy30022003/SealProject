using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusToMentorAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        //    migrationBuilder.DropColumn(
        //        name: "IsAccepted",
        //        table: "TeamInvitations");

        //    migrationBuilder.AddColumn<string>(
        //        name: "Status",
        //        table: "TeamInvitations",
        //        type: "nvarchar(max)",
        //        nullable: false,
        //        defaultValue: "");

        //    migrationBuilder.AddColumn<string>(
        //        name: "Status",
        //        table: "MentorAssignments",
        //        type: "nvarchar(max)",
        //        nullable: false,
        //        defaultValue: "");

        //    migrationBuilder.AddUniqueConstraint(
        //        name: "AK_Season_Code",
        //        table: "Season",
        //        column: "Code");

        //    migrationBuilder.CreateIndex(
        //        name: "IX_Hackathons_Season",
        //        table: "Hackathons",
        //        column: "Season");

        //    migrationBuilder.AddForeignKey(
        //        name: "FK_Hackathons_Season_Season",
        //        table: "Hackathons",
        //        column: "Season",
        //        principalTable: "Season",
        //        principalColumn: "Code");
        //}

        ///// <inheritdoc />
        //protected override void Down(MigrationBuilder migrationBuilder)
        //{
        //    migrationBuilder.DropForeignKey(
        //        name: "FK_Hackathons_Season_Season",
        //        table: "Hackathons");

        //    migrationBuilder.DropUniqueConstraint(
        //        name: "AK_Season_Code",
        //        table: "Season");

        //    migrationBuilder.DropIndex(
        //        name: "IX_Hackathons_Season",
        //        table: "Hackathons");

        //    migrationBuilder.DropColumn(
        //        name: "Status",
        //        table: "TeamInvitations");

        //    migrationBuilder.DropColumn(
        //        name: "Status",
        //        table: "MentorAssignments");

        //    migrationBuilder.AddColumn<bool>(
        //        name: "IsAccepted",
        //        table: "TeamInvitations",
        //        type: "bit",
        //        nullable: false,
        //        defaultValue: false);
        }
    }
}
