using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSubmissionAndTeamChallenge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK__Submissio__Hacka__59063A47",
                table: "Submissions");

            migrationBuilder.DropForeignKey(
                name: "FK__Submissio__TeamI__5812160E",
                table: "Submissions");

            migrationBuilder.DropForeignKey(
                name: "FK_TeamChallenges_Challenges_ChallengeID",
                table: "TeamChallenges");

            migrationBuilder.DropForeignKey(
                name: "FK_TeamChallenges_Teams_TeamID",
                table: "TeamChallenges");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TeamChallenges",
                table: "TeamChallenges");

            migrationBuilder.DropIndex(
                name: "IX_TeamChallenges_ChallengeID",
                table: "TeamChallenges");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Submissi__449EE1057EB597C1",
                table: "Submissions");

            migrationBuilder.RenameColumn(
                name: "ChallengeID",
                table: "TeamChallenges",
                newName: "PhaseId");

            migrationBuilder.RenameColumn(
                name: "HackathonID",
                table: "Submissions",
                newName: "HackathonId");

            migrationBuilder.RenameIndex(
                name: "IX_Submissions_HackathonID",
                table: "Submissions",
                newName: "IX_Submissions_HackathonId");

            migrationBuilder.AddColumn<int>(
                name: "TeamChallengeID",
                table: "TeamChallenges",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<int>(
                name: "HackathonID",
                table: "TeamChallenges",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Status",
                table: "TeamChallenges",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "TeamID",
                table: "Submissions",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "SubmittedAt",
                table: "Submissions",
                type: "datetime",
                nullable: false,
                defaultValueSql: "(getdate())",
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true,
                oldDefaultValueSql: "(getdate())");

            migrationBuilder.AlterColumn<string>(
                name: "ReportLink",
                table: "Submissions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "GitHubLink",
                table: "Submissions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DemoLink",
                table: "Submissions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFinal",
                table: "Submissions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PhaseID",
                table: "Submissions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SubmittedBy",
                table: "Submissions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TeamChallenges",
                table: "TeamChallenges",
                column: "TeamChallengeID");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Submission",
                table: "Submissions",
                column: "SubmissionID");

            migrationBuilder.CreateIndex(
                name: "IX_TeamChallenges_HackathonID",
                table: "TeamChallenges",
                column: "HackathonID");

            migrationBuilder.CreateIndex(
                name: "IX_TeamChallenges_TeamID",
                table: "TeamChallenges",
                column: "TeamID");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_PhaseID",
                table: "Submissions",
                column: "PhaseID");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_SubmittedBy",
                table: "Submissions",
                column: "SubmittedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_Submission_Phase",
                table: "Submissions",
                column: "PhaseID",
                principalTable: "HackathonPhases",
                principalColumn: "PhaseID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Submission_Team",
                table: "Submissions",
                column: "TeamID",
                principalTable: "Teams",
                principalColumn: "TeamID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Submission_User",
                table: "Submissions",
                column: "SubmittedBy",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Submissions_Hackathons_HackathonId",
                table: "Submissions",
                column: "HackathonId",
                principalTable: "Hackathons",
                principalColumn: "HackathonID");

            migrationBuilder.AddForeignKey(
                name: "FK_TeamChallenges_Hackathons",
                table: "TeamChallenges",
                column: "HackathonID",
                principalTable: "Hackathons",
                principalColumn: "HackathonID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TeamChallenges_Teams",
                table: "TeamChallenges",
                column: "TeamID",
                principalTable: "Teams",
                principalColumn: "TeamID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Submission_Phase",
                table: "Submissions");

            migrationBuilder.DropForeignKey(
                name: "FK_Submission_Team",
                table: "Submissions");

            migrationBuilder.DropForeignKey(
                name: "FK_Submission_User",
                table: "Submissions");

            migrationBuilder.DropForeignKey(
                name: "FK_Submissions_Hackathons_HackathonId",
                table: "Submissions");

            migrationBuilder.DropForeignKey(
                name: "FK_TeamChallenges_Hackathons",
                table: "TeamChallenges");

            migrationBuilder.DropForeignKey(
                name: "FK_TeamChallenges_Teams",
                table: "TeamChallenges");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TeamChallenges",
                table: "TeamChallenges");

            migrationBuilder.DropIndex(
                name: "IX_TeamChallenges_HackathonID",
                table: "TeamChallenges");

            migrationBuilder.DropIndex(
                name: "IX_TeamChallenges_TeamID",
                table: "TeamChallenges");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Submission",
                table: "Submissions");

            migrationBuilder.DropIndex(
                name: "IX_Submissions_PhaseID",
                table: "Submissions");

            migrationBuilder.DropIndex(
                name: "IX_Submissions_SubmittedBy",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "TeamChallengeID",
                table: "TeamChallenges");

            migrationBuilder.DropColumn(
                name: "HackathonID",
                table: "TeamChallenges");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "TeamChallenges");

            migrationBuilder.DropColumn(
                name: "IsFinal",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "PhaseID",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "SubmittedBy",
                table: "Submissions");

            migrationBuilder.RenameColumn(
                name: "PhaseId",
                table: "TeamChallenges",
                newName: "ChallengeID");

            migrationBuilder.RenameColumn(
                name: "HackathonId",
                table: "Submissions",
                newName: "HackathonID");

            migrationBuilder.RenameIndex(
                name: "IX_Submissions_HackathonId",
                table: "Submissions",
                newName: "IX_Submissions_HackathonID");

            migrationBuilder.AlterColumn<int>(
                name: "TeamID",
                table: "Submissions",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SubmittedAt",
                table: "Submissions",
                type: "datetime",
                nullable: true,
                defaultValueSql: "(getdate())",
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldDefaultValueSql: "(getdate())");

            migrationBuilder.AlterColumn<string>(
                name: "ReportLink",
                table: "Submissions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "GitHubLink",
                table: "Submissions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DemoLink",
                table: "Submissions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TeamChallenges",
                table: "TeamChallenges",
                columns: new[] { "TeamID", "ChallengeID" });

            migrationBuilder.AddPrimaryKey(
                name: "PK__Submissi__449EE1057EB597C1",
                table: "Submissions",
                column: "SubmissionID");

            migrationBuilder.CreateIndex(
                name: "IX_TeamChallenges_ChallengeID",
                table: "TeamChallenges",
                column: "ChallengeID");

            migrationBuilder.AddForeignKey(
                name: "FK__Submissio__Hacka__59063A47",
                table: "Submissions",
                column: "HackathonID",
                principalTable: "Hackathons",
                principalColumn: "HackathonID");

            migrationBuilder.AddForeignKey(
                name: "FK__Submissio__TeamI__5812160E",
                table: "Submissions",
                column: "TeamID",
                principalTable: "Teams",
                principalColumn: "TeamID");

            migrationBuilder.AddForeignKey(
                name: "FK_TeamChallenges_Challenges_ChallengeID",
                table: "TeamChallenges",
                column: "ChallengeID",
                principalTable: "Challenges",
                principalColumn: "ChallengeID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TeamChallenges_Teams_TeamID",
                table: "TeamChallenges",
                column: "TeamID",
                principalTable: "Teams",
                principalColumn: "TeamID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
