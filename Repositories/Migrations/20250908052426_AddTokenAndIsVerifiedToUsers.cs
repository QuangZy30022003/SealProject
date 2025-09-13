using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddTokenAndIsVerifiedToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.CreateTable(
            //    name: "Roles",
            //    columns: table => new
            //    {
            //        RoleID = table.Column<int>(type: "int", nullable: false)
            //            .Annotation("SqlServer:Identity", "1, 1"),
            //        RoleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK__Roles__8AFACE3AF3807AC9", x => x.RoleID);
            //    });

            //migrationBuilder.CreateTable(
            //    name: "Users",
            //    columns: table => new
            //    {
            //        UserID = table.Column<int>(type: "int", nullable: false)
            //            .Annotation("SqlServer:Identity", "1, 1"),
            //        FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
            //        Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
            //        PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
            //        RoleID = table.Column<int>(type: "int", nullable: true),
            //        CreatedAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
            //        Token = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //        IsVerified = table.Column<bool>(type: "bit", nullable: false)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK__Users__1788CCAC23E1D430", x => x.UserID);
            //        table.ForeignKey(
            //            name: "FK__Users__RoleID__3B75D760",
            //            column: x => x.RoleID,
            //            principalTable: "Roles",
            //            principalColumn: "RoleID");
            //    });

            //migrationBuilder.CreateTable(
            //    name: "AuditLogs",
            //    columns: table => new
            //    {
            //        LogID = table.Column<int>(type: "int", nullable: false)
            //            .Annotation("SqlServer:Identity", "1, 1"),
            //        UserID = table.Column<int>(type: "int", nullable: true),
            //        Action = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
            //        Details = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //        CreatedAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK__AuditLog__5E5499A88B2190AC", x => x.LogID);
            //        table.ForeignKey(
            //            name: "FK__AuditLogs__UserI__787EE5A0",
            //            column: x => x.UserID,
            //            principalTable: "Users",
            //            principalColumn: "UserID");
            //    });

            //migrationBuilder.CreateTable(
            //    name: "Chapters",
            //    columns: table => new
            //    {
            //        ChapterID = table.Column<int>(type: "int", nullable: false)
            //            .Annotation("SqlServer:Identity", "1, 1"),
            //        ChapterName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
            //        Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //        LeaderID = table.Column<int>(type: "int", nullable: true),
            //        CreatedAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK__Chapters__0893A34A6DB27A5A", x => x.ChapterID);
            //        table.ForeignKey(
            //            name: "FK__Chapters__Leader__3F466844",
            //            column: x => x.LeaderID,
            //            principalTable: "Users",
            //            principalColumn: "UserID");
            //    });

            //migrationBuilder.CreateTable(
            //    name: "Hackathons",
            //    columns: table => new
            //    {
            //        HackathonID = table.Column<int>(type: "int", nullable: false)
            //            .Annotation("SqlServer:Identity", "1, 1"),
            //        Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
            //        Season = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
            //        Theme = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
            //        StartDate = table.Column<DateOnly>(type: "date", nullable: true),
            //        EndDate = table.Column<DateOnly>(type: "date", nullable: true),
            //        CreatedBy = table.Column<int>(type: "int", nullable: true)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK__Hackatho__A9C9EEEB230112AC", x => x.HackathonID);
            //        table.ForeignKey(
            //            name: "FK__Hackathon__Creat__4CA06362",
            //            column: x => x.CreatedBy,
            //            principalTable: "Users",
            //            principalColumn: "UserID");
            //    });

            //migrationBuilder.CreateTable(
            //    name: "Notifications",
            //    columns: table => new
            //    {
            //        NotificationID = table.Column<int>(type: "int", nullable: false)
            //            .Annotation("SqlServer:Identity", "1, 1"),
            //        UserID = table.Column<int>(type: "int", nullable: true),
            //        Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
            //        SentAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
            //        IsRead = table.Column<bool>(type: "bit", nullable: true, defaultValue: false)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK__Notifica__20CF2E32A811D042", x => x.NotificationID);
            //        table.ForeignKey(
            //            name: "FK__Notificat__UserI__73BA3083",
            //            column: x => x.UserID,
            //            principalTable: "Users",
            //            principalColumn: "UserID");
            //    });

            //migrationBuilder.CreateTable(
            //    name: "Teams",
            //    columns: table => new
            //    {
            //        TeamID = table.Column<int>(type: "int", nullable: false)
            //            .Annotation("SqlServer:Identity", "1, 1"),
            //        TeamName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
            //        ChapterID = table.Column<int>(type: "int", nullable: true),
            //        LeaderID = table.Column<int>(type: "int", nullable: true),
            //        CreatedAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK__Teams__123AE7B9A0616BCD", x => x.TeamID);
            //        table.ForeignKey(
            //            name: "FK__Teams__ChapterID__4316F928",
            //            column: x => x.ChapterID,
            //            principalTable: "Chapters",
            //            principalColumn: "ChapterID",
            //            onDelete: ReferentialAction.Cascade);
            //        table.ForeignKey(
            //            name: "FK__Teams__LeaderID__440B1D61",
            //            column: x => x.LeaderID,
            //            principalTable: "Users",
            //            principalColumn: "UserID");
            //    });

            //migrationBuilder.CreateTable(
            //    name: "Criteria",
            //    columns: table => new
            //    {
            //        CriteriaID = table.Column<int>(type: "int", nullable: false)
            //            .Annotation("SqlServer:Identity", "1, 1"),
            //        HackathonID = table.Column<int>(type: "int", nullable: true),
            //        Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
            //        Weight = table.Column<decimal>(type: "decimal(5,2)", nullable: false)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK__Criteria__FE6ADB2DD6CC5237", x => x.CriteriaID);
            //        table.ForeignKey(
            //            name: "FK__Criteria__Hackat__5CD6CB2B",
            //            column: x => x.HackathonID,
            //            principalTable: "Hackathons",
            //            principalColumn: "HackathonID",
            //            onDelete: ReferentialAction.Cascade);
            //    });

            //migrationBuilder.CreateTable(
            //    name: "HackathonPhases",
            //    columns: table => new
            //    {
            //        PhaseID = table.Column<int>(type: "int", nullable: false)
            //            .Annotation("SqlServer:Identity", "1, 1"),
            //        HackathonID = table.Column<int>(type: "int", nullable: true),
            //        PhaseName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
            //        StartDate = table.Column<DateTime>(type: "datetime", nullable: false),
            //        EndDate = table.Column<DateTime>(type: "datetime", nullable: false)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK__Hackatho__5BA26D42DAB2AC98", x => x.PhaseID);
            //        table.ForeignKey(
            //            name: "FK__Hackathon__Hacka__4F7CD00D",
            //            column: x => x.HackathonID,
            //            principalTable: "Hackathons",
            //            principalColumn: "HackathonID",
            //            onDelete: ReferentialAction.Cascade);
            //    });

            //migrationBuilder.CreateTable(
            //    name: "Prizes",
            //    columns: table => new
            //    {
            //        PrizeID = table.Column<int>(type: "int", nullable: false)
            //            .Annotation("SqlServer:Identity", "1, 1"),
            //        HackathonID = table.Column<int>(type: "int", nullable: true),
            //        PrizeName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
            //        PrizeType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
            //        Rank = table.Column<int>(type: "int", nullable: true),
            //        Reward = table.Column<string>(type: "nvarchar(max)", nullable: true)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK__Prizes__5C36F4BB25F6A3D0", x => x.PrizeID);
            //        table.ForeignKey(
            //            name: "FK__Prizes__Hackatho__6B24EA82",
            //            column: x => x.HackathonID,
            //            principalTable: "Hackathons",
            //            principalColumn: "HackathonID");
            //    });

            //migrationBuilder.CreateTable(
            //    name: "MentorAssignments",
            //    columns: table => new
            //    {
            //        AssignmentID = table.Column<int>(type: "int", nullable: false)
            //            .Annotation("SqlServer:Identity", "1, 1"),
            //        MentorID = table.Column<int>(type: "int", nullable: true),
            //        ChapterID = table.Column<int>(type: "int", nullable: true),
            //        TeamID = table.Column<int>(type: "int", nullable: true),
            //        AssignedAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK__MentorAs__32499E570D49BF4A", x => x.AssignmentID);
            //        table.ForeignKey(
            //            name: "FK__MentorAss__Chapt__534D60F1",
            //            column: x => x.ChapterID,
            //            principalTable: "Chapters",
            //            principalColumn: "ChapterID");
            //        table.ForeignKey(
            //            name: "FK__MentorAss__Mento__52593CB8",
            //            column: x => x.MentorID,
            //            principalTable: "Users",
            //            principalColumn: "UserID");
            //        table.ForeignKey(
            //            name: "FK__MentorAss__TeamI__5441852A",
            //            column: x => x.TeamID,
            //            principalTable: "Teams",
            //            principalColumn: "TeamID");
            //    });

            //migrationBuilder.CreateTable(
            //    name: "PenaltiesBonuses",
            //    columns: table => new
            //    {
            //        AdjustmentID = table.Column<int>(type: "int", nullable: false)
            //            .Annotation("SqlServer:Identity", "1, 1"),
            //        TeamID = table.Column<int>(type: "int", nullable: true),
            //        HackathonID = table.Column<int>(type: "int", nullable: true),
            //        Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
            //        Points = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
            //        Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //        CreatedAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK__Penaltie__E60DB8B32307C0EE", x => x.AdjustmentID);
            //        table.ForeignKey(
            //            name: "FK__Penalties__Hacka__66603565",
            //            column: x => x.HackathonID,
            //            principalTable: "Hackathons",
            //            principalColumn: "HackathonID");
            //        table.ForeignKey(
            //            name: "FK__Penalties__TeamI__656C112C",
            //            column: x => x.TeamID,
            //            principalTable: "Teams",
            //            principalColumn: "TeamID");
            //    });

            //migrationBuilder.CreateTable(
            //    name: "Submissions",
            //    columns: table => new
            //    {
            //        SubmissionID = table.Column<int>(type: "int", nullable: false)
            //            .Annotation("SqlServer:Identity", "1, 1"),
            //        TeamID = table.Column<int>(type: "int", nullable: true),
            //        HackathonID = table.Column<int>(type: "int", nullable: true),
            //        Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
            //        GitHubLink = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //        DemoLink = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //        ReportLink = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //        SubmittedAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK__Submissi__449EE1057EB597C1", x => x.SubmissionID);
            //        table.ForeignKey(
            //            name: "FK__Submissio__Hacka__59063A47",
            //            column: x => x.HackathonID,
            //            principalTable: "Hackathons",
            //            principalColumn: "HackathonID");
            //        table.ForeignKey(
            //            name: "FK__Submissio__TeamI__5812160E",
            //            column: x => x.TeamID,
            //            principalTable: "Teams",
            //            principalColumn: "TeamID");
            //    });

            //migrationBuilder.CreateTable(
            //    name: "TeamMembers",
            //    columns: table => new
            //    {
            //        TeamID = table.Column<int>(type: "int", nullable: false),
            //        UserID = table.Column<int>(type: "int", nullable: false),
            //        RoleInTeam = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, defaultValue: "Member")
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK__TeamMemb__C3426B73A96BB544", x => new { x.TeamID, x.UserID });
            //        table.ForeignKey(
            //            name: "FK__TeamMembe__TeamI__47DBAE45",
            //            column: x => x.TeamID,
            //            principalTable: "Teams",
            //            principalColumn: "TeamID",
            //            onDelete: ReferentialAction.Cascade);
            //        table.ForeignKey(
            //            name: "FK__TeamMembe__UserI__48CFD27E",
            //            column: x => x.UserID,
            //            principalTable: "Users",
            //            principalColumn: "UserID",
            //            onDelete: ReferentialAction.Cascade);
            //    });

            //migrationBuilder.CreateTable(
            //    name: "PrizeAllocations",
            //    columns: table => new
            //    {
            //        AllocationID = table.Column<int>(type: "int", nullable: false)
            //            .Annotation("SqlServer:Identity", "1, 1"),
            //        PrizeID = table.Column<int>(type: "int", nullable: true),
            //        TeamID = table.Column<int>(type: "int", nullable: true),
            //        UserID = table.Column<int>(type: "int", nullable: true),
            //        AwardedAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK__PrizeAll__B3C6D6ABB124F70B", x => x.AllocationID);
            //        table.ForeignKey(
            //            name: "FK__PrizeAllo__Prize__6E01572D",
            //            column: x => x.PrizeID,
            //            principalTable: "Prizes",
            //            principalColumn: "PrizeID");
            //        table.ForeignKey(
            //            name: "FK__PrizeAllo__TeamI__6EF57B66",
            //            column: x => x.TeamID,
            //            principalTable: "Teams",
            //            principalColumn: "TeamID");
            //        table.ForeignKey(
            //            name: "FK__PrizeAllo__UserI__6FE99F9F",
            //            column: x => x.UserID,
            //            principalTable: "Users",
            //            principalColumn: "UserID");
            //    });

            //migrationBuilder.CreateTable(
            //    name: "Scores",
            //    columns: table => new
            //    {
            //        ScoreID = table.Column<int>(type: "int", nullable: false)
            //            .Annotation("SqlServer:Identity", "1, 1"),
            //        SubmissionID = table.Column<int>(type: "int", nullable: true),
            //        JudgeID = table.Column<int>(type: "int", nullable: true),
            //        CriteriaID = table.Column<int>(type: "int", nullable: true),
            //        Score = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
            //        Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //        ScoredAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK__Scores__7DD229F11B6F7D35", x => x.ScoreID);
            //        table.ForeignKey(
            //            name: "FK__Scores__Criteria__619B8048",
            //            column: x => x.CriteriaID,
            //            principalTable: "Criteria",
            //            principalColumn: "CriteriaID");
            //        table.ForeignKey(
            //            name: "FK__Scores__JudgeID__60A75C0F",
            //            column: x => x.JudgeID,
            //            principalTable: "Users",
            //            principalColumn: "UserID");
            //        table.ForeignKey(
            //            name: "FK__Scores__Submissi__5FB337D6",
            //            column: x => x.SubmissionID,
            //            principalTable: "Submissions",
            //            principalColumn: "SubmissionID",
            //            onDelete: ReferentialAction.Cascade);
            //    });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserID",
                table: "AuditLogs",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_LeaderID",
                table: "Chapters",
                column: "LeaderID");

            migrationBuilder.CreateIndex(
                name: "IX_Criteria_HackathonID",
                table: "Criteria",
                column: "HackathonID");

            migrationBuilder.CreateIndex(
                name: "IX_HackathonPhases_HackathonID",
                table: "HackathonPhases",
                column: "HackathonID");

            migrationBuilder.CreateIndex(
                name: "IX_Hackathons_CreatedBy",
                table: "Hackathons",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_MentorAssignments_ChapterID",
                table: "MentorAssignments",
                column: "ChapterID");

            migrationBuilder.CreateIndex(
                name: "IX_MentorAssignments_MentorID",
                table: "MentorAssignments",
                column: "MentorID");

            migrationBuilder.CreateIndex(
                name: "IX_MentorAssignments_TeamID",
                table: "MentorAssignments",
                column: "TeamID");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserID",
                table: "Notifications",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_PenaltiesBonuses_HackathonID",
                table: "PenaltiesBonuses",
                column: "HackathonID");

            migrationBuilder.CreateIndex(
                name: "IX_PenaltiesBonuses_TeamID",
                table: "PenaltiesBonuses",
                column: "TeamID");

            migrationBuilder.CreateIndex(
                name: "IX_PrizeAllocations_PrizeID",
                table: "PrizeAllocations",
                column: "PrizeID");

            migrationBuilder.CreateIndex(
                name: "IX_PrizeAllocations_TeamID",
                table: "PrizeAllocations",
                column: "TeamID");

            migrationBuilder.CreateIndex(
                name: "IX_PrizeAllocations_UserID",
                table: "PrizeAllocations",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_Prizes_HackathonID",
                table: "Prizes",
                column: "HackathonID");

            migrationBuilder.CreateIndex(
                name: "UQ__Roles__8A2B6160D48421E9",
                table: "Roles",
                column: "RoleName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Scores_CriteriaID",
                table: "Scores",
                column: "CriteriaID");

            migrationBuilder.CreateIndex(
                name: "IX_Scores_JudgeID",
                table: "Scores",
                column: "JudgeID");

            migrationBuilder.CreateIndex(
                name: "IX_Scores_SubmissionID",
                table: "Scores",
                column: "SubmissionID");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_HackathonID",
                table: "Submissions",
                column: "HackathonID");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_TeamID",
                table: "Submissions",
                column: "TeamID");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_UserID",
                table: "TeamMembers",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_ChapterID",
                table: "Teams",
                column: "ChapterID");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_LeaderID",
                table: "Teams",
                column: "LeaderID");

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleID",
                table: "Users",
                column: "RoleID");

            migrationBuilder.CreateIndex(
                name: "UQ__Users__A9D10534190B6398",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "HackathonPhases");

            migrationBuilder.DropTable(
                name: "MentorAssignments");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "PenaltiesBonuses");

            migrationBuilder.DropTable(
                name: "PrizeAllocations");

            migrationBuilder.DropTable(
                name: "Scores");

            migrationBuilder.DropTable(
                name: "TeamMembers");

            migrationBuilder.DropTable(
                name: "Prizes");

            migrationBuilder.DropTable(
                name: "Criteria");

            migrationBuilder.DropTable(
                name: "Submissions");

            migrationBuilder.DropTable(
                name: "Hackathons");

            migrationBuilder.DropTable(
                name: "Teams");

            migrationBuilder.DropTable(
                name: "Chapters");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
