using FluentAssertions;
using Moq;
using NUnit.Framework;
using Repositories.Interface;
using Repositories.Models;
using Repositories.UnitOfWork;
using Service.Servicefolder;
using System.Linq.Expressions;

namespace NUniTest.Service
{
    [TestFixture]
    public class RoleServiceTests
    {
        private Mock<IUOW> _uowMock;
        private RoleService _service;

        private Mock<IRepository<Role>> _roleRepo;

        [SetUp]
        public void Setup()
        {
            _uowMock = new Mock<IUOW>();
            _roleRepo = new Mock<IRepository<Role>>();

            _uowMock.Setup(u => u.Roles).Returns(_roleRepo.Object);

            _service = new RoleService(_uowMock.Object);
        }

        // =============================
        // 1. CreateRoleAsync - Role name is null
        // =============================
        [Test]
        public void CreateRoleAsync_WhenRoleNameIsNull_Throws()
        {
            Func<Task> act = async () => await _service.CreateRoleAsync(null);

            act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Role name cannot be null or empty. (Parameter 'roleName')");
        }

        // =============================
        // 2. CreateRoleAsync - Role name is empty
        // =============================
        [Test]
        public void CreateRoleAsync_WhenRoleNameIsEmpty_Throws()
        {
            Func<Task> act = async () => await _service.CreateRoleAsync("");

            act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Role name cannot be null or empty. (Parameter 'roleName')");
        }

        // =============================
        // 3. CreateRoleAsync - Role name is whitespace
        // =============================
        [Test]
        public void CreateRoleAsync_WhenRoleNameIsWhitespace_Throws()
        {
            Func<Task> act = async () => await _service.CreateRoleAsync("   ");

            act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Role name cannot be null or empty. (Parameter 'roleName')");
        }

        // =============================
        // 4. CreateRoleAsync - Role already exists (case insensitive)
        // =============================
        [Test]
        public void CreateRoleAsync_WhenRoleAlreadyExists_Throws()
        {
            var existingRoles = new List<Role>
            {
                new Role { RoleId = 1, RoleName = "Admin" },
                new Role { RoleId = 2, RoleName = "User" }
            };

            _roleRepo.Setup(r => r.GetAllAsync(
        It.IsAny<Expression<Func<Role, bool>>>(),
        It.IsAny<Func<IQueryable<Role>, IOrderedQueryable<Role>>>(),
        It.IsAny<string>()
    ))
    .ReturnsAsync(existingRoles);

            Func<Task> act = async () => await _service.CreateRoleAsync("ADMIN"); // Different case

            act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Role 'ADMIN' already exists.");
        }

        // =============================
        // 5. CreateRoleAsync - Successful creation
        // =============================
        [Test]
        public async Task CreateRoleAsync_WhenValidRoleName_ShouldCreateSuccessfully()
        {
            var existingRoles = new List<Role>
            {
                new Role { RoleId = 1, RoleName = "Admin" },
                new Role { RoleId = 2, RoleName = "User" }
            };
            var newRole = new Role { RoleId = 3, RoleName = "Moderator" };

            _roleRepo.Setup(r => r.GetAllAsync(
        It.IsAny<Expression<Func<Role, bool>>>(),
        It.IsAny<Func<IQueryable<Role>, IOrderedQueryable<Role>>>(),
        It.IsAny<string>()
    ))
    .ReturnsAsync(existingRoles);
            _roleRepo.Setup(r => r.AddAsync(It.IsAny<Role>())).Returns(Task.CompletedTask);
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            var result = await _service.CreateRoleAsync("Moderator");

            result.Should().NotBeNull();
            result.RoleName.Should().Be("Moderator");

            _roleRepo.Verify(r => r.AddAsync(It.Is<Role>(role => role.RoleName == "Moderator")), Times.Once);
            _uowMock.Verify(u => u.SaveAsync(It.IsAny<int?>()), Times.AtLeastOnce());
        }

        // =============================
        // 6. CreateRoleAsync - Create role with different case than existing
        // =============================
        [Test]
        public async Task CreateRoleAsync_WhenDifferentCaseFromExisting_ShouldCreateSuccessfully()
        {
            var existingRoles = new List<Role>
            {
                new Role { RoleId = 1, RoleName = "admin" } // lowercase
            };

            _roleRepo.Setup(r => r.GetAllAsync(
        It.IsAny<Expression<Func<Role, bool>>>(),
        It.IsAny<Func<IQueryable<Role>, IOrderedQueryable<Role>>>(),
        It.IsAny<string>()
    ))
    .ReturnsAsync(existingRoles);
            _roleRepo.Setup(r => r.AddAsync(It.IsAny<Role>())).Returns(Task.CompletedTask);
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            var result = await _service.CreateRoleAsync("Manager");

            result.Should().NotBeNull();
            result.RoleName.Should().Be("Manager");

            _roleRepo.Verify(r => r.AddAsync(It.Is<Role>(role => role.RoleName == "Manager")), Times.Once);
        }

        // =============================
        // 7. GetAllRolesAsync - Return empty list
        // =============================
        [Test]
        public async Task GetAllRolesAsync_WhenNoRoles_ShouldReturnEmptyList()
        {
            var emptyRoles = new List<Role>();

            _roleRepo.Setup(r => r.GetAllAsync(
        It.IsAny<Expression<Func<Role, bool>>>(),
        It.IsAny<Func<IQueryable<Role>, IOrderedQueryable<Role>>>(),
        It.IsAny<string>()
    ))
    .ReturnsAsync(emptyRoles);

            var result = await _service.GetAllRolesAsync();

            result.Should().BeEmpty();
        }

        // =============================
        // 8. GetAllRolesAsync - Return all roles
        // =============================
        [Test]
        public async Task GetAllRolesAsync_WhenRolesExist_ShouldReturnAllRoles()
        {
            var roles = new List<Role>
            {
                new Role { RoleId = 1, RoleName = "Admin" },
                new Role { RoleId = 2, RoleName = "User" },
                new Role { RoleId = 3, RoleName = "Moderator" }
            };

            _roleRepo.Setup(r => r.GetAllAsync(
        It.IsAny<Expression<Func<Role, bool>>>(),
        It.IsAny<Func<IQueryable<Role>, IOrderedQueryable<Role>>>(),
        It.IsAny<string>()
    ))
    .ReturnsAsync(roles);

            var result = await _service.GetAllRolesAsync();

            result.Should().HaveCount(3);
            result.Should().Contain(r => r.RoleName == "Admin");
            result.Should().Contain(r => r.RoleName == "User");
            result.Should().Contain(r => r.RoleName == "Moderator");
        }

        // =============================
        // 9. CreateRoleAsync - Role name with special characters
        // =============================
        [Test]
        public async Task CreateRoleAsync_WithSpecialCharacters_ShouldCreateSuccessfully()
        {
            var existingRoles = new List<Role>();

            _roleRepo.Setup(r => r.GetAllAsync(
        It.IsAny<Expression<Func<Role, bool>>>(),
        It.IsAny<Func<IQueryable<Role>, IOrderedQueryable<Role>>>(),
        It.IsAny<string>()
    ))
    .ReturnsAsync(existingRoles);
            _roleRepo.Setup(r => r.AddAsync(It.IsAny<Role>())).Returns(Task.CompletedTask);
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            var result = await _service.CreateRoleAsync("Super-Admin_2024");

            result.Should().NotBeNull();
            result.RoleName.Should().Be("Super-Admin_2024");

            _roleRepo.Verify(r => r.AddAsync(It.Is<Role>(role => role.RoleName == "Super-Admin_2024")), Times.Once);
        }

        // =============================
        // 10. CreateRoleAsync - Role name with leading/trailing spaces
        // =============================
        [Test]
        public async Task CreateRoleAsync_WithLeadingTrailingSpaces_ShouldCreateSuccessfully()
        {
            var existingRoles = new List<Role>();

            _roleRepo.Setup(r => r.GetAllAsync(
        It.IsAny<Expression<Func<Role, bool>>>(),
        It.IsAny<Func<IQueryable<Role>, IOrderedQueryable<Role>>>(),
        It.IsAny<string>()
    ))
    .ReturnsAsync(existingRoles);

            _roleRepo.Setup(r => r.AddAsync(It.IsAny<Role>())).Returns(Task.CompletedTask);
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            var result = await _service.CreateRoleAsync("  Manager  ");

            result.Should().NotBeNull();
            result.RoleName.Should().Be("  Manager  "); // Preserves the spaces as provided

            _roleRepo.Verify(r => r.AddAsync(It.Is<Role>(role => role.RoleName == "  Manager  ")), Times.Once);
        }

        // =============================
        // 11. CreateRoleAsync - Case sensitivity check with mixed case
        // =============================
        [Test]
        public void CreateRoleAsync_WhenSameRoleWithMixedCase_Throws()
        {
            var existingRoles = new List<Role>
            {
                new Role { RoleId = 1, RoleName = "TeamLeader" }
            };

            _roleRepo.Setup(r => r.GetAllAsync(
        It.IsAny<Expression<Func<Role, bool>>>(),
        It.IsAny<Func<IQueryable<Role>, IOrderedQueryable<Role>>>(),
        It.IsAny<string>()
    ))
    .ReturnsAsync(existingRoles);


            Func<Task> act = async () => await _service.CreateRoleAsync("teamleader"); // Different case

            act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Role 'teamleader' already exists.");
        }

        // =============================
        // 12. GetAllRolesAsync - Verify repository call
        // =============================
        [Test]
        public async Task GetAllRolesAsync_ShouldCallRepositoryOnce()
        {
            var roles = new List<Role>
            {
                new Role { RoleId = 1, RoleName = "Admin" }
            };

            _roleRepo.Setup(r => r.GetAllAsync(
        It.IsAny<Expression<Func<Role, bool>>>(),
        It.IsAny<Func<IQueryable<Role>, IOrderedQueryable<Role>>>(),
        It.IsAny<string>()
    ))
    .ReturnsAsync(roles);

            await _service.GetAllRolesAsync();

            _roleRepo.Verify(r => r.GetAllAsync(It.IsAny<Expression<Func<Role, bool>>>(), It.IsAny<Func<IQueryable<Role>, IOrderedQueryable<Role>>>(), It.IsAny<string>()), Times.Once);
        }
    }
}