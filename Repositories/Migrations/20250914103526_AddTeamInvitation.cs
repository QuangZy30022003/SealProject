using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamInvitation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        //    migrationBuilder.DropForeignKey(
        //        name: "FK_Challenge_Season_SeasonID",
        //        table: "Challenge");

        //    migrationBuilder.DropForeignKey(
        //        name: "FK_Challenge_Users_UserID",
        //        table: "Challenge");

        //    migrationBuilder.DropForeignKey(
        //        name: "FK_TeamChallenge_Challenge_ChallengeID",
        //        table: "TeamChallenge");

        //    migrationBuilder.DropForeignKey(
        //        name: "FK_TeamChallenge_Teams_TeamID",
        //        table: "TeamChallenge");

        //    migrationBuilder.DropPrimaryKey(
        //        name: "PK_Seasons",
        //        table: "Season");

        //    migrationBuilder.RenameTable(
        //        name: "TeamChallenge",
        //        newName: "TeamChallenges");

        //    migrationBuilder.RenameTable(
        //        name: "Challenge",
        //        newName: "Challenges");

        //    migrationBuilder.RenameIndex(
        //        name: "IX_TeamChallenge_ChallengeID",
        //        table: "TeamChallenges",
        //        newName: "IX_TeamChallenges_ChallengeID");

        //    migrationBuilder.RenameIndex(
        //        name: "IX_Challenge_UserID",
        //        table: "Challenges",
        //        newName: "IX_Challenges_UserID");

        //    migrationBuilder.RenameIndex(
        //        name: "IX_Challenge_SeasonID",
        //        table: "Challenges",
        //        newName: "IX_Challenges_SeasonID");

        //    migrationBuilder.AddPrimaryKey(
        //        name: "PK_Season",
        //        table: "Season",
        //        column: "SeasonID");

        //    migrationBuilder.CreateTable(
        //        name: "TeamInvitations",
        //        columns: table => new
        //        {
        //            InvitationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
        //            TeamId = table.Column<int>(type: "int", nullable: false),
        //            InvitedEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
        //            InvitedByUserId = table.Column<int>(type: "int", nullable: false),
        //            InvitationCode = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
        //            IsAccepted = table.Column<bool>(type: "bit", nullable: false),
        //            CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
        //            ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false)
        //        },
        //        constraints: table =>
        //        {
        //            table.PrimaryKey("PK_TeamInvitations", x => x.InvitationId);
        //            table.ForeignKey(
        //                name: "FK_TeamInvitations_Teams_TeamId",
        //                column: x => x.TeamId,
        //                principalTable: "Teams",
        //                principalColumn: "TeamID",
        //                onDelete: ReferentialAction.Cascade);
        //            table.ForeignKey(
        //                name: "FK_TeamInvitations_Users_InvitedByUserId",
        //                column: x => x.InvitedByUserId,
        //                principalTable: "Users",
        //                principalColumn: "UserID",
        //                onDelete: ReferentialAction.Cascade);
        //        });

        //    migrationBuilder.CreateIndex(
        //        name: "IX_TeamInvitations_InvitedByUserId",
        //        table: "TeamInvitations",
        //        column: "InvitedByUserId");

        //    migrationBuilder.CreateIndex(
        //        name: "IX_TeamInvitations_TeamId",
        //        table: "TeamInvitations",
        //        column: "TeamId");

        //    migrationBuilder.AddForeignKey(
        //        name: "FK_Challenges_Season_SeasonID",
        //        table: "Challenges",
        //        column: "SeasonID",
        //        principalTable: "Season",
        //        principalColumn: "SeasonID",
        //        onDelete: ReferentialAction.Cascade);

        //    migrationBuilder.AddForeignKey(
        //        name: "FK_Challenges_Users_UserID",
        //        table: "Challenges",
        //        column: "UserID",
        //        principalTable: "Users",
        //        principalColumn: "UserID",
        //        onDelete: ReferentialAction.Restrict);

        //    migrationBuilder.AddForeignKey(
        //        name: "FK_TeamChallenges_Challenges_ChallengeID",
        //        table: "TeamChallenges",
        //        column: "ChallengeID",
        //        principalTable: "Challenges",
        //        principalColumn: "ChallengeID",
        //        onDelete: ReferentialAction.Cascade);

        //    migrationBuilder.AddForeignKey(
        //        name: "FK_TeamChallenges_Teams_TeamID",
        //        table: "TeamChallenges",
        //        column: "TeamID",
        //        principalTable: "Teams",
        //        principalColumn: "TeamID",
        //        onDelete: ReferentialAction.Cascade);
        //}

        ///// <inheritdoc />
        //protected override void Down(MigrationBuilder migrationBuilder)
        //{
        //    migrationBuilder.DropForeignKey(
        //        name: "FK_Challenges_Season_SeasonID",
        //        table: "Challenges");

        //    migrationBuilder.DropForeignKey(
        //        name: "FK_Challenges_Users_UserID",
        //        table: "Challenges");

        //    migrationBuilder.DropForeignKey(
        //        name: "FK_TeamChallenges_Challenges_ChallengeID",
        //        table: "TeamChallenges");

        //    migrationBuilder.DropForeignKey(
        //        name: "FK_TeamChallenges_Teams_TeamID",
        //        table: "TeamChallenges");

        //    migrationBuilder.DropTable(
        //        name: "TeamInvitations");

        //    migrationBuilder.DropPrimaryKey(
        //        name: "PK_Season",
        //        table: "Season");

        //    migrationBuilder.RenameTable(
        //        name: "TeamChallenges",
        //        newName: "TeamChallenge");

        //    migrationBuilder.RenameTable(
        //        name: "Challenges",
        //        newName: "Challenge");

        //    migrationBuilder.RenameIndex(
        //        name: "IX_TeamChallenges_ChallengeID",
        //        table: "TeamChallenge",
        //        newName: "IX_TeamChallenge_ChallengeID");

        //    migrationBuilder.RenameIndex(
        //        name: "IX_Challenges_UserID",
        //        table: "Challenge",
        //        newName: "IX_Challenge_UserID");

        //    migrationBuilder.RenameIndex(
        //        name: "IX_Challenges_SeasonID",
        //        table: "Challenge",
        //        newName: "IX_Challenge_SeasonID");

        //    migrationBuilder.AddPrimaryKey(
        //        name: "PK_Seasons",
        //        table: "Season",
        //        column: "SeasonID");

        //    migrationBuilder.AddForeignKey(
        //        name: "FK_Challenge_Season_SeasonID",
        //        table: "Challenge",
        //        column: "SeasonID",
        //        principalTable: "Season",
        //        principalColumn: "SeasonID",
        //        onDelete: ReferentialAction.Cascade);

        //    migrationBuilder.AddForeignKey(
        //        name: "FK_Challenge_Users_UserID",
        //        table: "Challenge",
        //        column: "UserID",
        //        principalTable: "Users",
        //        principalColumn: "UserID",
        //        onDelete: ReferentialAction.Restrict);

        //    migrationBuilder.AddForeignKey(
        //        name: "FK_TeamChallenge_Challenge_ChallengeID",
        //        table: "TeamChallenge",
        //        column: "ChallengeID",
        //        principalTable: "Challenge",
        //        principalColumn: "ChallengeID",
        //        onDelete: ReferentialAction.Cascade);

        //    migrationBuilder.AddForeignKey(
        //        name: "FK_TeamChallenge_Teams_TeamID",
        //        table: "TeamChallenge",
        //        column: "TeamID",
        //        principalTable: "Teams",
        //        principalColumn: "TeamID",
        //        onDelete: ReferentialAction.Cascade);
        }
    }
}
