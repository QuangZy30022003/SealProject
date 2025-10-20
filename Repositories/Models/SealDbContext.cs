using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Repositories.Models;

public partial class SealDbContext : DbContext
{
    public SealDbContext()
    {
    }

    public SealDbContext(DbContextOptions<SealDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Chapter> Chapters { get; set; }

    public virtual DbSet<Criterion> Criteria { get; set; }

    public virtual DbSet<Hackathon> Hackathons { get; set; }

    public virtual DbSet<HackathonPhase> HackathonPhases { get; set; }

    public virtual DbSet<Leaderboard> Leaderboards { get; set; }

    public virtual DbSet<MentorAssignment> MentorAssignments { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<PenaltiesBonuse> PenaltiesBonuses { get; set; }

    public virtual DbSet<Prize> Prizes { get; set; }

    public virtual DbSet<PrizeAllocation> PrizeAllocations { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Score> Scores { get; set; }

    public virtual DbSet<Submission> Submissions { get; set; }

    public virtual DbSet<Team> Teams { get; set; }

    public virtual DbSet<TeamMember> TeamMembers { get; set; }

    public virtual DbSet<Season> Seasons { get; set; }

    public virtual DbSet<Challenge> Challenges { get; set; }

    public virtual DbSet<TeamChallenge> TeamChallenges { get; set; }

    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<TeamInvitation> TeamInvitations { get; set; }
    public virtual DbSet<StudentVerification> StudentVerifications { get; set; }

    public virtual DbSet<Appeal> Appeals { get; set; }
    public virtual DbSet<PhaseChallenge> PhaseChallenges { get; set; }



    //    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    //#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
    //        => optionsBuilder.UseSqlServer("Server=DESKTOP-IQK16CS\\QUANGZY;Database=Seal;User ID=sa;Password=12345;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__AuditLog__5E5499A88B2190AC");

            entity.Property(e => e.LogId).HasColumnName("LogID");
            entity.Property(e => e.Action).HasMaxLength(200);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__AuditLogs__UserI__787EE5A0");
        });

        modelBuilder.Entity<Chapter>(entity =>
        {
            entity.HasKey(e => e.ChapterId).HasName("PK__Chapters__0893A34A6DB27A5A");

            entity.Property(e => e.ChapterId).HasColumnName("ChapterID");
            entity.Property(e => e.ChapterName).HasMaxLength(100);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.LeaderId).HasColumnName("LeaderID");

            entity.HasOne(d => d.Leader).WithMany(p => p.Chapters)
                .HasForeignKey(d => d.LeaderId)
                .HasConstraintName("FK__Chapters__Leader__3F466844");
        });

        modelBuilder.Entity<Criterion>(entity =>
        {
            entity.HasKey(e => e.CriteriaId).HasName("PK__Criteria__FE6ADB2DD6CC5237");

            entity.Property(e => e.CriteriaId).HasColumnName("CriteriaID");
            entity.Property(e => e.PhaseChallengeId).HasColumnName("PhaseChallengeID");
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Weight).HasColumnType("decimal(5, 2)");

            entity.HasOne(d => d.PhaseChallenge)
                .WithMany(p => p.Criterion)
                .HasForeignKey(d => d.PhaseChallengeId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Criteria_PhaseChallenge");
        });

        modelBuilder.Entity<CriterionDetail>(entity =>
        {
            entity.ToTable("CriterionDetails");
            entity.HasKey(e => e.CriterionDetailId);
            entity.Property(e => e.Description).HasMaxLength(255);

            entity.HasOne(d => d.Criterion)
                  .WithMany(p => p.CriterionDetails)
                  .HasForeignKey(d => d.CriteriaId)
                  .OnDelete(DeleteBehavior.Cascade);
        });


        modelBuilder.Entity<Hackathon>(entity =>
        {
            entity.HasKey(e => e.HackathonId).HasName("PK__Hackatho__A9C9EEEBC3FD865C");

            entity.HasIndex(e => e.CreatedBy, "IX_Hackathons_CreatedBy");

            entity.Property(e => e.HackathonId).HasColumnName("HackathonID");
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.Season).HasMaxLength(20);
            entity.Property(e => e.Theme).HasMaxLength(200);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Hackathons)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK__Hackathon__Creat__3A81B327");
        });

        modelBuilder.Entity<HackathonPhase>(entity =>
        {
            entity.HasKey(e => e.PhaseId).HasName("PK__Hackatho__5BA26D42DF28EC72");

            entity.HasIndex(e => e.HackathonId, "IX_HackathonPhases_HackathonID");

            entity.Property(e => e.PhaseId).HasColumnName("PhaseID");
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.HackathonId).HasColumnName("HackathonID");
            entity.Property(e => e.PhaseName)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.StartDate).HasColumnType("datetime");

            entity.HasOne(d => d.Hackathon).WithMany(p => p.HackathonPhases)
                .HasForeignKey(d => d.HackathonId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Hackathon__Hacka__3D5E1FD2");
        });
        modelBuilder.Entity<Leaderboard>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("Leaderboard");

            entity.Property(e => e.HackathonId).HasColumnName("HackathonID");
            entity.Property(e => e.TeamId).HasColumnName("TeamID");
            entity.Property(e => e.TeamName).HasMaxLength(100);
            entity.Property(e => e.TotalScore).HasColumnType("decimal(38, 2)");
        });

        modelBuilder.Entity<MentorAssignment>(entity =>
        {
            entity.HasKey(e => e.AssignmentId).HasName("PK__MentorAs__32499E570D49BF4A");

            entity.Property(e => e.AssignmentId).HasColumnName("AssignmentID");
            entity.Property(e => e.AssignedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ChapterId).HasColumnName("ChapterID");
            entity.Property(e => e.MentorId).HasColumnName("MentorID");
            entity.Property(e => e.TeamId).HasColumnName("TeamID");

            entity.HasOne(d => d.Chapter).WithMany(p => p.MentorAssignments)
                .HasForeignKey(d => d.ChapterId)
                .HasConstraintName("FK__MentorAss__Chapt__534D60F1");

            entity.HasOne(d => d.Mentor).WithMany(p => p.MentorAssignments)
                .HasForeignKey(d => d.MentorId)
                .HasConstraintName("FK__MentorAss__Mento__52593CB8");

            entity.HasOne(d => d.Team).WithMany(p => p.MentorAssignments)
                .HasForeignKey(d => d.TeamId)
                .HasConstraintName("FK__MentorAss__TeamI__5441852A");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__20CF2E32A811D042");

            entity.Property(e => e.NotificationId).HasColumnName("NotificationID");
            entity.Property(e => e.IsRead).HasDefaultValue(false);
            entity.Property(e => e.SentAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Notificat__UserI__73BA3083");
        });

        modelBuilder.Entity<PenaltiesBonuse>(entity =>
        {
            entity.HasKey(e => e.AdjustmentId).HasName("PK__Penaltie__E60DB8B32307C0EE");

            entity.Property(e => e.AdjustmentId).HasColumnName("AdjustmentID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.HackathonId).HasColumnName("HackathonID");
            entity.Property(e => e.Points).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.TeamId).HasColumnName("TeamID");
            entity.Property(e => e.Type).HasMaxLength(20);

            entity.HasOne(d => d.Hackathon).WithMany(p => p.PenaltiesBonuses)
                .HasForeignKey(d => d.HackathonId)
                .HasConstraintName("FK__Penalties__Hacka__66603565");

            entity.HasOne(d => d.Team).WithMany(p => p.PenaltiesBonuses)
                .HasForeignKey(d => d.TeamId)
                .HasConstraintName("FK__Penalties__TeamI__656C112C");
        });

        modelBuilder.Entity<Prize>(entity =>
        {
            entity.HasKey(e => e.PrizeId).HasName("PK__Prizes__5C36F4BB25F6A3D0");

            entity.Property(e => e.PrizeId).HasColumnName("PrizeID");
            entity.Property(e => e.HackathonId).HasColumnName("HackathonID");
            entity.Property(e => e.PrizeName).HasMaxLength(100);
            entity.Property(e => e.PrizeType).HasMaxLength(50);

            entity.HasOne(d => d.Hackathon).WithMany(p => p.Prizes)
                .HasForeignKey(d => d.HackathonId)
                .HasConstraintName("FK__Prizes__Hackatho__6B24EA82");
        });

        modelBuilder.Entity<PrizeAllocation>(entity =>
        {
            entity.HasKey(e => e.AllocationId).HasName("PK__PrizeAll__B3C6D6ABB124F70B");

            entity.Property(e => e.AllocationId).HasColumnName("AllocationID");
            entity.Property(e => e.AwardedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PrizeId).HasColumnName("PrizeID");
            entity.Property(e => e.TeamId).HasColumnName("TeamID");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Prize).WithMany(p => p.PrizeAllocations)
                .HasForeignKey(d => d.PrizeId)
                .HasConstraintName("FK__PrizeAllo__Prize__6E01572D");

            entity.HasOne(d => d.Team).WithMany(p => p.PrizeAllocations)
                .HasForeignKey(d => d.TeamId)
                .HasConstraintName("FK__PrizeAllo__TeamI__6EF57B66");

            entity.HasOne(d => d.User).WithMany(p => p.PrizeAllocations)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__PrizeAllo__UserI__6FE99F9F");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE3AF3807AC9");

            entity.HasIndex(e => e.RoleName, "UQ__Roles__8A2B6160D48421E9").IsUnique();

            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<Score>(entity =>
        {
            entity.HasKey(e => e.ScoreId).HasName("PK__Scores__7DD229F11B6F7D35");

            entity.Property(e => e.ScoreId).HasColumnName("ScoreID");
            entity.Property(e => e.CriteriaId).HasColumnName("CriteriaID");
            entity.Property(e => e.JudgeId).HasColumnName("JudgeID");
            entity.Property(e => e.Score1)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("Score");
            entity.Property(e => e.ScoredAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.SubmissionId).HasColumnName("SubmissionID");

            entity.HasOne(d => d.Criteria).WithMany(p => p.Scores)
                .HasForeignKey(d => d.CriteriaId)
                .HasConstraintName("FK__Scores__Criteria__619B8048");

            entity.HasOne(d => d.Judge).WithMany(p => p.Scores)
                .HasForeignKey(d => d.JudgeId)
                .HasConstraintName("FK__Scores__JudgeID__60A75C0F");

            entity.HasOne(d => d.Submission).WithMany(p => p.Scores)
                .HasForeignKey(d => d.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Scores__Submissi__5FB337D6");
        });
        modelBuilder.Entity<Submission>(entity =>
        {
            entity.HasKey(e => e.SubmissionId).HasName("PK__Submissi__449EE1050C9EA9BB");

            entity.HasIndex(e => e.TeamId, "IX_Submissions_TeamID");

            entity.Property(e => e.SubmissionId).HasColumnName("SubmissionID");
            entity.Property(e => e.SubmittedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TeamId).HasColumnName("TeamID");
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.SubmittedByNavigation).WithMany(p => p.Submissions)
                .HasForeignKey(d => d.SubmittedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Submission_User");

            entity.HasOne(d => d.Team).WithMany(p => p.Submissions)
                .HasForeignKey(d => d.TeamId)
                .HasConstraintName("FK__Submissio__TeamI__45F365D3");
        });

        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasKey(e => e.TeamId).HasName("PK__Teams__123AE7B9A0616BCD");

            entity.Property(e => e.TeamId).HasColumnName("TeamID");
            entity.Property(e => e.ChapterId).HasColumnName("ChapterID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.LeaderId).HasColumnName("LeaderID");
            entity.Property(e => e.TeamName).HasMaxLength(100);

            entity.HasOne(d => d.Chapter).WithMany(p => p.Teams)
                .HasForeignKey(d => d.ChapterId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Teams__ChapterID__4316F928");

            entity.HasOne(d => d.Leader).WithMany(p => p.Teams)
                .HasForeignKey(d => d.LeaderId)
                .HasConstraintName("FK__Teams__LeaderID__440B1D61");
        });

        modelBuilder.Entity<TeamMember>(entity =>
        {
            entity.HasKey(e => new { e.TeamId, e.UserId }).HasName("PK__TeamMemb__C3426B73A96BB544");

            entity.ToTable(tb => tb.HasTrigger("trg_CheckTeamSize"));

            entity.Property(e => e.TeamId).HasColumnName("TeamID");
            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.RoleInTeam)
                .HasMaxLength(50)
                .HasDefaultValue("Member");

            entity.HasOne(d => d.Team).WithMany(p => p.TeamMembers)
                .HasForeignKey(d => d.TeamId)
                .HasConstraintName("FK__TeamMembe__TeamI__47DBAE45");

            entity.HasOne(d => d.User).WithMany(p => p.TeamMembers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__TeamMembe__UserI__48CFD27E");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CCAC23E1D430");

            entity.HasIndex(e => e.Email, "UQ__Users__A9D10534190B6398").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.RoleId).HasColumnName("RoleID");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("FK__Users__RoleID__3B75D760");
        });

        modelBuilder.Entity<Season>(entity =>
        {
            entity.ToTable("Season"); 

            entity.HasKey(e => e.SeasonId).HasName("PK_Season");
            entity.Property(e => e.SeasonId).HasColumnName("SeasonID");
            entity.Property(e => e.Code).HasMaxLength(20);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.StartDate);
            entity.Property(e => e.EndDate);
        });


        modelBuilder.Entity<Challenge>(entity =>
        {
            entity.HasKey(e => e.ChallengeId).HasName("PK_Challenges");
            entity.Property(e => e.ChallengeId).HasColumnName("ChallengeID");
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.FilePath).HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("(getdate())");

            entity.Property(e => e.SeasonId).HasColumnName("SeasonID");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Season).WithMany(p => p.Challenges)
                  .HasForeignKey(d => d.SeasonId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.User).WithMany(p => p.Challenges)
                  .HasForeignKey(d => d.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // TeamChallenge
        modelBuilder.Entity<TeamChallenge>(entity =>
        {
            entity.HasKey(e => e.TeamChallengeId)
                  .HasName("PK_TeamChallenges");

            entity.Property(e => e.TeamChallengeId)
                  .HasColumnName("TeamChallengeID");

            entity.Property(e => e.TeamId)
                  .HasColumnName("TeamID");

            entity.Property(e => e.HackathonId)
                  .HasColumnName("HackathonID");

            entity.Property(e => e.RegisteredAt)
                  .HasColumnType("datetime")
                  .HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Team)
                  .WithMany(p => p.TeamChallenges)
                  .HasForeignKey(d => d.TeamId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("FK_TeamChallenges_Teams");

            entity.HasOne(d => d.Hackathon)
                  .WithMany(p => p.TeamChallenges)
                  .HasForeignKey(d => d.HackathonId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("FK_TeamChallenges_Hackathons");
        });

        //team invitation
        modelBuilder.Entity<TeamInvitation>(entity =>
        {
            entity.HasKey(t => t.InvitationId);
            entity.Property(t => t.InvitedEmail).IsRequired();
            entity.Property(t => t.InvitationCode).IsRequired();
        });
        modelBuilder.Entity<StudentVerification>(entity =>
        {
            entity.HasKey(e => e.VerificationId)
                  .HasName("PK_StudentVerification");

            entity.Property(e => e.VerificationId)
                  .HasColumnName("VerificationID");

            entity.Property(e => e.UserId)
                  .HasColumnName("UserID");

            entity.Property(e => e.UniversityName)
                  .HasMaxLength(200)
                  .IsRequired();

            entity.Property(e => e.StudentEmail)
                  .HasMaxLength(150)
                  .IsRequired();

            entity.Property(e => e.StudentCode)
                  .HasMaxLength(50)
                  .IsRequired();

            entity.Property(e => e.FullName)
                  .HasMaxLength(150)
                  .IsRequired();

            entity.Property(e => e.Major)
                  .HasMaxLength(150);

            entity.Property(e => e.Status)
                  .HasMaxLength(50)
                  .HasDefaultValue("Pending");

            entity.Property(e => e.CreatedAt)
                  .HasColumnType("datetime")
                  .HasDefaultValueSql("(getdate())");

            entity.Property(e => e.UpdatedAt)
                  .HasColumnType("datetime")
                  .HasDefaultValueSql("(getdate())");

            // Quan hệ 1-1 hoặc 1-n với User
            entity.HasOne(d => d.User)
                  .WithMany(p => p.StudentVerifications)
                  .HasForeignKey(d => d.UserId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("FK_StudentVerification_User");
        });
        modelBuilder.Entity<PhaseChallenge>(entity =>
        {
            entity.HasKey(e => e.PhaseChallengeId).HasName("PK__PhaseCha__9A75B672A3020308");

            entity.Property(e => e.AssignedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.Challenge).WithMany(p => p.PhaseChallenges)
                .HasForeignKey(d => d.ChallengeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PhaseChallenges_Challenge");

            entity.HasOne(d => d.Phase).WithMany(p => p.PhaseChallenges)
                .HasForeignKey(d => d.PhaseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PhaseChallenges_HackathonPhases");
        });



        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
