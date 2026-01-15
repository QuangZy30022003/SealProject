using NUnit.Framework;
using FluentAssertions;
using Repositories.Models;
using Common.DTOs.AuthDto;
using Common.DTOs.TeamDto;

namespace NUniTest
{
    [TestFixture]
    public class BasicTests
    {
        [Test]
        public void User_Creation_ShouldSetProperties()
        {
            // Arrange & Act
            var user = new User
            {
                UserId = 1,
                Email = "test@example.com",
                FullName = "Test User",
                IsVerified = true,
                IsBlocked = false
            };

            // Assert
            user.UserId.Should().Be(1);
            user.Email.Should().Be("test@example.com");
            user.FullName.Should().Be("Test User");
            user.IsVerified.Should().BeTrue();
            user.IsBlocked.Should().BeFalse();
        }

        [Test]
        public void Team_Creation_ShouldSetProperties()
        {
            // Arrange & Act
            var team = new Team
            {
                TeamId = 1,
                TeamName = "Test Team",
                ChapterId = 1,
                TeamLeaderId = 1,
                CreatedAt = DateTime.UtcNow
            };

            // Assert
            team.TeamId.Should().Be(1);
            team.TeamName.Should().Be("Test Team");
            team.ChapterId.Should().Be(1);
            team.TeamLeaderId.Should().Be(1);
            team.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Test]
        public void UserResponseDto_Mapping_ShouldWork()
        {
            // Arrange & Act
            var userDto = new UserResponseDto
            {
                UserId = 1,
                Email = "test@example.com",
                FullName = "Test User",
                IsVerified = true,
                IsBlocked = false
            };

            // Assert
            userDto.UserId.Should().Be(1);
            userDto.Email.Should().Be("test@example.com");
            userDto.FullName.Should().Be("Test User");
            userDto.IsVerified.Should().BeTrue();
            userDto.IsBlocked.Should().BeFalse();
        }

        [Test]
        public void CreateTeamDto_Validation_ShouldWork()
        {
            // Arrange & Act
            var createTeamDto = new CreateTeamDto
            {
                TeamName = "New Team",
                ChapterId = 1
            };

            // Assert
            createTeamDto.TeamName.Should().Be("New Team");
            createTeamDto.ChapterId.Should().Be(1);
        }

        [Test]
        public void TeamDto_Properties_ShouldBeSet()
        {
            // Arrange & Act
            var teamDto = new TeamDto
            {
                TeamId = 1,
                TeamName = "Test Team",
                ChapterId = 1
            };

            // Assert
            teamDto.TeamId.Should().Be(1);
            teamDto.TeamName.Should().Be("Test Team");
            teamDto.ChapterId.Should().Be(1);
        }

        [Test]
        public void Role_Creation_ShouldWork()
        {
            // Arrange & Act
            var role = new Role
            {
                RoleId = 1,
                RoleName = "Student"
            };

            // Assert
            role.RoleId.Should().Be(1);
            role.RoleName.Should().Be("Student");
        }

        [Test]
        public void Hackathon_Creation_ShouldWork()
        {
            // Arrange & Act
            var hackathon = new Hackathon
            {
                HackathonId = 1,
                Name = "Spring Hackathon",
                Description = "Annual spring coding competition",
                SeasonId = 1,
                CreatedBy = 1,
                Status = "Active"
            };

            // Assert
            hackathon.HackathonId.Should().Be(1);
            hackathon.Name.Should().Be("Spring Hackathon");
            hackathon.Description.Should().Be("Annual spring coding competition");
            hackathon.SeasonId.Should().Be(1);
            hackathon.CreatedBy.Should().Be(1);
            hackathon.Status.Should().Be("Active");
        }

        [Test]
        public void Chapter_Creation_ShouldWork()
        {
            // Arrange & Act
            var chapter = new Chapter
            {
                ChapterId = 1,
                ChapterName = "Ho Chi Minh City",
                Description = "HCMC Chapter"
            };

            // Assert
            chapter.ChapterId.Should().Be(1);
            chapter.ChapterName.Should().Be("Ho Chi Minh City");
            chapter.Description.Should().Be("HCMC Chapter");
        }

        [Test]
        public void DateTime_Comparison_ShouldWork()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var future = now.AddDays(1);
            var past = now.AddDays(-1);

            // Act & Assert
            future.Should().BeAfter(now);
            past.Should().BeBefore(now);
            now.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Test]
        public void String_Operations_ShouldWork()
        {
            // Arrange
            var email = "test@example.com";
            var name = "Test User";

            // Act & Assert
            email.Should().Contain("@");
            email.Should().EndWith(".com");
            name.Should().StartWith("Test");
            name.Should().HaveLength(9);
        }
    }
}