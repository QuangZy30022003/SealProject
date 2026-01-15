using AutoMapper;
using Common.DTOs.AuthDto;
using Common.Helper;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using Repositories.Interface;
using Repositories.Models;
using Repositories.UnitOfWork;
using Service.Interface;
using Service.Servicefolder;
using System.Linq.Expressions;

namespace NUniTest.Service
{
    [TestFixture]
    public class AuthServiceTests
    {
        private Mock<IUOW> _uowMock;
        private Mock<IEmailService> _emailServiceMock;
        private JwtHelper _jwtHelper;
        private Mock<IMapper> _mapperMock;
        private Mock<IConfiguration> _configurationMock;
        private AuthService _service;

        private Mock<IRepository<User>> _userRepo;
        private Mock<IRepository<Role>> _roleRepo;
        private Mock<IRepository<PartnerProfile>> _partnerProfileRepo;

        [SetUp]
        public void Setup()
        {
            _uowMock = new Mock<IUOW>();
            _emailServiceMock = new Mock<IEmailService>();
            _configurationMock = new Mock<IConfiguration>();
            // Setup configuration for JwtHelper
            var jwtSection = new Mock<IConfigurationSection>();
            jwtSection.Setup(s => s["Key"]).Returns("ThisIsASecretKeyForJWTTokenGenerationThatIsAtLeast32CharactersLong");
            jwtSection.Setup(s => s["Issuer"]).Returns("TestIssuer");
            jwtSection.Setup(s => s["Audience"]).Returns("TestAudience");
            jwtSection.Setup(s => s["ExpiryInMinutes"]).Returns("60");
            
            _configurationMock.Setup(c => c.GetSection("JwtSettings")).Returns(jwtSection.Object);
            
            _jwtHelper = new JwtHelper(_configurationMock.Object, _uowMock.Object);
            _mapperMock = new Mock<IMapper>();

            _userRepo = new Mock<IRepository<User>>();
            _roleRepo = new Mock<IRepository<Role>>();
            _partnerProfileRepo = new Mock<IRepository<PartnerProfile>>();

            _uowMock.Setup(u => u.Users).Returns(_userRepo.Object);
            _uowMock.Setup(u => u.Roles).Returns(_roleRepo.Object);
            _uowMock.Setup(u => u.PartnerProfiles).Returns(_partnerProfileRepo.Object);

            _service = new AuthService(_uowMock.Object, _emailServiceMock.Object, _jwtHelper, _mapperMock.Object, _configurationMock.Object);
        }

        // =============================
        // 1. LoginWithGoogleAsync - User bị block
        // =============================
        [Test]
        public void LoginWithGoogleAsync_WhenUserBlocked_Throws()
        {
            var email = "blocked@test.com";
            var blockedUser = new User { UserId = 1, Email = email, IsBlocked = true };

            _userRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                    .ReturnsAsync(blockedUser);

            Func<Task> act = async () => await _service.LoginWithGoogleAsync(email);

            act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Your account has been blocked by the administrator.");
        }

        // =============================
        // 2. LoginWithGoogleAsync - User mới, tạo account và gửi email verification
        // =============================
        [Test]
        public async Task LoginWithGoogleAsync_WhenNewUser_ShouldCreateUserAndSendEmail()
        {
            var email = "newuser@test.com";
            var accessToken = "access_token_123";
            var refreshToken = "refresh_token_456";

            _userRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                    .ReturnsAsync((User)null);

            _userRepo.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            // JwtHelper will generate real token

            _emailServiceMock.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                            .Returns(Task.CompletedTask);

            _userRepo.Setup(r => r.Update(It.IsAny<User>()));

            var result = await _service.LoginWithGoogleAsync(email);

            result.accessToken.Should().NotBeNullOrEmpty();
            result.isVerified.Should().BeFalse(); // New user chưa verify

            _userRepo.Verify(r => r.AddAsync(It.Is<User>(u => 
                u.Email == email && 
                u.IsVerified == false && 
                u.RoleId == 1)), Times.Once);

            _emailServiceMock.Verify(e => e.SendEmailAsync(
                email, 
                "Verify your email", 
                It.Is<string>(body => body.Contains("Verify Email"))), Times.Once);
        }

        // =============================
        // 3. LoginWithGoogleAsync - User đã tồn tại, login thành công
        // =============================
        [Test]
        public async Task LoginWithGoogleAsync_WhenExistingUser_ShouldLoginSuccessfully()
        {
            var email = "existing@test.com";
            var existingUser = new User 
            { 
                UserId = 1, 
                Email = email, 
                IsBlocked = false, 
                IsVerified = true,
                FullName = "Existing User"
            };
            var accessToken = "access_token_123";
            var refreshToken = "refresh_token_456";

            _userRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                    .ReturnsAsync(existingUser);

            // JwtHelper will generate real token

            _userRepo.Setup(r => r.Update(It.IsAny<User>()));
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            var result = await _service.LoginWithGoogleAsync(email);

            result.accessToken.Should().NotBeNullOrEmpty();
            result.isVerified.Should().BeTrue();

            existingUser.RefreshToken.Should().NotBeNull();
            existingUser.RefreshTokenExpiryTime.Should().BeAfter(DateTime.UtcNow);

            _userRepo.Verify(r => r.Update(existingUser), Times.Once);
            _emailServiceMock.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        // =============================
        // 4. LogoutAsync - User không tồn tại
        // =============================
        [Test]
        public async Task LogoutAsync_WhenUserNotFound_ReturnsFalse()
        {
            _userRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User)null);

            var result = await _service.LogoutAsync(99);

            result.Should().BeFalse();
        }

        // =============================
        // 5. LogoutAsync - Logout thành công
        // =============================
        [Test]
        public async Task LogoutAsync_WhenUserExists_ShouldClearTokensAndReturnTrue()
        {
            var user = new User 
            { 
                UserId = 1, 
                RefreshToken = "some_token", 
                RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(1) 
            };

            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
            _userRepo.Setup(r => r.Update(It.IsAny<User>()));
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            var result = await _service.LogoutAsync(1);

            result.Should().BeTrue();
            user.RefreshToken.Should().BeNull();
            user.RefreshTokenExpiryTime.Should().BeNull();

            _userRepo.Verify(r => r.Update(user), Times.Once);
        }

        // =============================
        // 6. RefreshTokenAsync - Token không hợp lệ
        // =============================
        [Test]
        public void RefreshTokenAsync_WhenInvalidToken_Throws()
        {
            var refreshToken = "invalid_token";

            _userRepo.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<User, bool>>>(), null, null))
                    .ReturnsAsync(new List<User>());

            Func<Task> act = async () => await _service.RefreshTokenAsync(refreshToken);

            act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Invalid refresh token");
        }

        // =============================
        // 7. RefreshTokenAsync - Token hết hạn
        // =============================
        [Test]
        public void RefreshTokenAsync_WhenTokenExpired_Throws()
        {
            var refreshToken = "expired_token";
            var user = new User 
            { 
                UserId = 1, 
                RefreshToken = refreshToken, 
                RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(-1) // Đã hết hạn
            };

            _userRepo.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<User, bool>>>(), null, null))
                    .ReturnsAsync(new List<User> { user });

            Func<Task> act = async () => await _service.RefreshTokenAsync(refreshToken);

            act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Invalid refresh token");
        }
        // =============================
        // 8. RefreshTokenAsync - Refresh thành công
        // =============================
        [Test]
        public async Task RefreshTokenAsync_WhenValidToken_ShouldReturnNewTokens()
        {
            var refreshToken = "valid_token";
            var user = new User 
            { 
                UserId = 1, 
                RefreshToken = refreshToken, 
                RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(1) // Còn hạn
            };
            var newAccessToken = "new_access_token";
            var newRefreshToken = "new_refresh_token";

            _userRepo.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<User, bool>>>(), null, null))
                    .ReturnsAsync(new List<User> { user });

            // JwtHelper will generate real tokens

            _userRepo.Setup(r => r.Update(It.IsAny<User>()));
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            var result = await _service.RefreshTokenAsync(refreshToken);

            result.accessToken.Should().NotBeNullOrEmpty();
            result.refreshToken.Should().NotBeNullOrEmpty();

            user.RefreshToken.Should().NotBeNullOrEmpty();
            user.RefreshTokenExpiryTime.Should().BeAfter(DateTime.UtcNow);
        }

        // =============================
        // 9. VerifyEmailAsync - Token rỗng
        // =============================
        [Test]
        public void VerifyEmailAsync_WhenEmptyToken_Throws()
        {
            Func<Task> act = async () => await _service.VerifyEmailAsync("");

            act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Invalid token.");
        }

        // =============================
        // 10. VerifyEmailAsync - Token không tồn tại
        // =============================
        [Test]
        public void VerifyEmailAsync_WhenTokenNotFound_Throws()
        {
            var token = "invalid_token";

            _userRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                    .ReturnsAsync((User)null);

            Func<Task> act = async () => await _service.VerifyEmailAsync(token);

            act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("User not found or token invalid.");
        }

        // =============================
        // 11. VerifyEmailAsync - User đã verify
        // =============================
        [Test]
        public async Task VerifyEmailAsync_WhenUserAlreadyVerified_ReturnsMessage()
        {
            var token = "valid_token";
            var user = new User { UserId = 1, Token = token, IsVerified = true };

            _userRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                    .ReturnsAsync(user);

            var result = await _service.VerifyEmailAsync(token);

            result.Should().Be("Your account is already verified.");
        }

        // =============================
        // 12. VerifyEmailAsync - Verify thành công
        // =============================
        [Test]
        public async Task VerifyEmailAsync_WhenValid_ShouldVerifyUser()
        {
            var token = "valid_token";
            var user = new User { UserId = 1, Token = token, IsVerified = false };

            _userRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                    .ReturnsAsync(user);

            _userRepo.Setup(r => r.Update(It.IsAny<User>()));
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            var result = await _service.VerifyEmailAsync(token);

            result.Should().Be("Email verified successfully. You can now log in.");
            user.IsVerified.Should().BeTrue();
            user.Token.Should().BeNull();

            _userRepo.Verify(r => r.Update(user), Times.Once);
        }

        // =============================
        // 13. GetUserByIdAsync - User không tồn tại
        // =============================
        [Test]
        public async Task GetUserByIdAsync_WhenUserNotFound_ReturnsNull()
        {
            _userRepo.Setup(r => r.GetByIdIncludingAsync(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Expression<Func<User, object>>>()))
                .ReturnsAsync((User)null);

            var result = await _service.GetUserByIdAsync(99);

            result.Should().BeNull();
        }

        // =============================
        // 14. GetUserByIdAsync - Lấy user thành công
        // =============================
        [Test]
        public async Task GetUserByIdAsync_WhenUserExists_ReturnsUserDto()
        {
            var user = new User { UserId = 1, FullName = "Test User", Email = "test@test.com" };
            var userDto = new UserResponseDto { UserId = 1, FullName = "Test User", Email = "test@test.com" };

            _userRepo.Setup(r => r.GetByIdIncludingAsync(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Expression<Func<User, object>>>()))
                .ReturnsAsync(user);

            _mapperMock.Setup(m => m.Map<UserResponseDto>(user)).Returns(userDto);

            var result = await _service.GetUserByIdAsync(1);

            result.Should().NotBeNull();
            result.UserId.Should().Be(1);
            result.FullName.Should().Be("Test User");
        }

        // =============================
        // 15. UpdateUserInfoAsync - User không tồn tại
        // =============================
        [Test]
        public async Task UpdateUserInfoAsync_WhenUserNotFound_ReturnsFalse()
        {
            var dto = new UpdateUserDto { FullName = "New Name" };

            _userRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User)null);

            var result = await _service.UpdateUserInfoAsync(99, dto);

            result.Should().BeFalse();
        }

        // =============================
        // 16. UpdateUserInfoAsync - User bị block
        // =============================
        [Test]
        public void UpdateUserInfoAsync_WhenUserBlocked_Throws()
        {
            var dto = new UpdateUserDto { FullName = "New Name" };
            var user = new User { UserId = 1, IsBlocked = true };

            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

            Func<Task> act = async () => await _service.UpdateUserInfoAsync(1, dto);

            act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Your account has been blocked by admin.");
        }

        // =============================
        // 17. UpdateUserInfoAsync - Update thành công
        // =============================
        [Test]
        public async Task UpdateUserInfoAsync_WhenValid_ShouldUpdateAndReturnTrue()
        {
            var dto = new UpdateUserDto { FullName = "Updated Name" };
            var user = new User { UserId = 1, FullName = "Old Name", IsBlocked = false };

            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
            _userRepo.Setup(r => r.Update(It.IsAny<User>()));
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            var result = await _service.UpdateUserInfoAsync(1, dto);

            result.Should().BeTrue();
            user.FullName.Should().Be("Updated Name");

            _userRepo.Verify(r => r.Update(user), Times.Once);
        }

        // =============================
        // 18. SetUserBlockedStatusAsync - User không tồn tại
        // =============================
        [Test]
        public async Task SetUserBlockedStatusAsync_WhenUserNotFound_ReturnsFalse()
        {
            _userRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User)null);

            var result = await _service.SetUserBlockedStatusAsync(99, true);

            result.Should().BeFalse();
        }

        // =============================
        // 19. SetUserBlockedStatusAsync - Set blocked status thành công
        // =============================
        [Test]
        public async Task SetUserBlockedStatusAsync_WhenValid_ShouldUpdateStatusAndReturnTrue()
        {
            var user = new User { UserId = 1, IsBlocked = false };

            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
            _userRepo.Setup(r => r.Update(It.IsAny<User>()));
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            var result = await _service.SetUserBlockedStatusAsync(1, true);

            result.Should().BeTrue();
            user.IsBlocked.Should().BeTrue();

            _userRepo.Verify(r => r.Update(user), Times.Once);
        }

        // =============================
        // 20. ChangeUserRoleAsync - User không tồn tại
        // =============================
        [Test]
        public async Task ChangeUserRoleAsync_WhenUserNotFound_ReturnsFalse()
        {
            _userRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User)null);

            var result = await _service.ChangeUserRoleAsync(99, 2);

            result.Should().BeFalse();
        }

        // =============================
        // 21. ChangeUserRoleAsync - Role không tồn tại
        // =============================
        [Test]
        public void ChangeUserRoleAsync_WhenRoleNotFound_Throws()
        {
            var user = new User { UserId = 1, RoleId = 1 };

            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
            _roleRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Role)null);

            Func<Task> act = async () => await _service.ChangeUserRoleAsync(1, 99);

            act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Role not found");
        }

        // =============================
        // 22. ChangeUserRoleAsync - Change role thành công
        // =============================
        [Test]
        public async Task ChangeUserRoleAsync_WhenValid_ShouldChangeRoleAndReturnTrue()
        {
            var user = new User { UserId = 1, RoleId = 1 };
            var role = new Role { RoleId = 2, RoleName = "Judge" };

            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
            _roleRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(role);

            _userRepo.Setup(r => r.Update(It.IsAny<User>()));
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            var result = await _service.ChangeUserRoleAsync(1, 2);

            result.Should().BeTrue();
            user.RoleId.Should().Be(2);

            _userRepo.Verify(r => r.Update(user), Times.Once);
        }

        #region Forget Password Tests

        // =============================
        // 23. ForgotPasswordAsync - User tồn tại
        // =============================
        [Test]
        public async Task ForgotPasswordAsync_WhenUserExists_ShouldSendEmailAndReturnTrue()
        {
            // Arrange
            var email = "test@example.com";
            var user = new User
            {
                UserId = 1,
                Email = email,
                FullName = "Test User",
                IsBlocked = false
            };

            _userRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(user);

            _emailServiceMock.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _userRepo.Setup(r => r.Update(It.IsAny<User>()));
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            // Act
            var result = await _service.ForgotPasswordAsync(email);

            // Assert
            result.Should().BeTrue();
            _userRepo.Verify(r => r.Update(It.Is<User>(u => u.Token != null)), Times.Once);
            _uowMock.Verify(u => u.SaveAsync(It.IsAny<int?>()), Times.Once);
            _emailServiceMock.Verify(e => e.SendEmailAsync(email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        // =============================
        // 24. ForgotPasswordAsync - User không tồn tại
        // =============================
        [Test]
        public async Task ForgotPasswordAsync_WhenUserNotExists_ShouldReturnFalse()
        {
            // Arrange
            var email = "nonexistent@example.com";

            _userRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync((User)null);

            // Act
            var result = await _service.ForgotPasswordAsync(email);

            // Assert
            result.Should().BeFalse();
            _userRepo.Verify(r => r.Update(It.IsAny<User>()), Times.Never);
            _emailServiceMock.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        // =============================
        // 25. ForgotPasswordAsync - User bị block
        // =============================
        [Test]
        public async Task ForgotPasswordAsync_WhenUserIsBlocked_ShouldReturnFalse()
        {
            // Arrange
            var email = "blocked@example.com";
            var user = new User
            {
                UserId = 1,
                Email = email,
                FullName = "Blocked User",
                IsBlocked = true
            };

            _userRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(user);

            // Act
            var result = await _service.ForgotPasswordAsync(email);

            // Assert
            result.Should().BeFalse();
            _userRepo.Verify(r => r.Update(It.IsAny<User>()), Times.Never);
            _emailServiceMock.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        // =============================
        // 26. ForgotPasswordAsync - Email service fails
        // =============================
        [Test]
        public async Task ForgotPasswordAsync_WhenEmailServiceFails_ShouldReturnFalse()
        {
            // Arrange
            var email = "test@example.com";
            var user = new User
            {
                UserId = 1,
                Email = email,
                FullName = "Test User",
                IsBlocked = false
            };

            _userRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(user);

            _emailServiceMock.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Email service error"));

            _userRepo.Setup(r => r.Update(It.IsAny<User>()));
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            // Act
            var result = await _service.ForgotPasswordAsync(email);

            // Assert
            result.Should().BeFalse();
        }

        // =============================
        // 27. ResetPasswordAsync - Token hợp lệ
        // =============================
        [Test]
        public async Task ResetPasswordAsync_WhenValidToken_ShouldResetPasswordAndReturnTrue()
        {
            // Arrange
            var token = "valid-reset-token";
            var newPassword = "newPassword123";
            var user = new User
            {
                UserId = 1,
                Email = "test@example.com",
                FullName = "Test User",
                Token = token,
                IsBlocked = false,
                RefreshToken = "old-refresh-token"
            };

            _userRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(user);

            _emailServiceMock.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _userRepo.Setup(r => r.Update(It.IsAny<User>()));
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            // Act
            var result = await _service.ResetPasswordAsync(token, newPassword);

            // Assert
            result.Should().BeTrue();
            _userRepo.Verify(r => r.Update(It.Is<User>(u => 
                u.PasswordHash != null && 
                u.Token == null && 
                u.RefreshToken == null)), Times.Once);
            _uowMock.Verify(u => u.SaveAsync(It.IsAny<int?>()), Times.Once);
            _emailServiceMock.Verify(e => e.SendEmailAsync(user.Email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        // =============================
        // 28. ResetPasswordAsync - Token không hợp lệ
        // =============================
        [Test]
        public async Task ResetPasswordAsync_WhenInvalidToken_ShouldReturnFalse()
        {
            // Arrange
            var token = "invalid-token";
            var newPassword = "newPassword123";

            _userRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync((User)null);

            // Act
            var result = await _service.ResetPasswordAsync(token, newPassword);

            // Assert
            result.Should().BeFalse();
            _userRepo.Verify(r => r.Update(It.IsAny<User>()), Times.Never);
            _emailServiceMock.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        // =============================
        // 29. ResetPasswordAsync - User bị block
        // =============================
        [Test]
        public async Task ResetPasswordAsync_WhenUserIsBlocked_ShouldReturnFalse()
        {
            // Arrange
            var token = "valid-reset-token";
            var newPassword = "newPassword123";
            var user = new User
            {
                UserId = 1,
                Email = "blocked@example.com",
                Token = token,
                IsBlocked = true
            };

            _userRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(user);

            // Act
            var result = await _service.ResetPasswordAsync(token, newPassword);

            // Assert
            result.Should().BeFalse();
            _userRepo.Verify(r => r.Update(It.IsAny<User>()), Times.Never);
        }

        // =============================
        // 30. ResetPasswordAsync - Token rỗng
        // =============================
        [Test]
        public async Task ResetPasswordAsync_WhenEmptyToken_ShouldReturnFalse()
        {
            // Arrange
            var token = "";
            var newPassword = "newPassword123";

            // Act
            var result = await _service.ResetPasswordAsync(token, newPassword);

            // Assert
            result.Should().BeFalse();
            _userRepo.Verify(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()), Times.Never);
        }

        // =============================
        // 31. ResetPasswordAsync - Password rỗng
        // =============================
        [Test]
        public async Task ResetPasswordAsync_WhenEmptyPassword_ShouldReturnFalse()
        {
            // Arrange
            var token = "valid-token";
            var newPassword = "";

            // Act
            var result = await _service.ResetPasswordAsync(token, newPassword);

            // Assert
            result.Should().BeFalse();
            _userRepo.Verify(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()), Times.Never);
        }

        #endregion
    }
}