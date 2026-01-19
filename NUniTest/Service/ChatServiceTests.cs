using AutoMapper;
using Common.DTOs.ChatDto;
using Common.Wrappers;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Moq;
using NUnit.Framework;
using Repositories.Interface;
using Repositories.Models;
using Repositories.UnitOfWork;
using Service.Hubs;
using Service.Servicefolder;
using System.Linq.Expressions;

namespace NUniTest.Service
{
    [TestFixture]
    public class ChatServiceTests
    {
        private Mock<IUOW> _uowMock;
        private Mock<IMapper> _mapperMock;
        private Mock<IHubContext<ChatHub>> _hubContextMock;
        private ChatService _service;

        private Mock<IRepository<ChatGroup>> _chatGroupRepo;
        private Mock<IRepository<ChatMessage>> _chatMessageRepo;
        private Mock<IRepository<ChatMessageRead>> _chatMessageReadRepo;
        private Mock<IRepository<TeamMember>> _teamMemberRepo;
        private Mock<IRepository<User>> _userRepo;

        [SetUp]
        public void Setup()
        {
            _uowMock = new Mock<IUOW>();
            _mapperMock = new Mock<IMapper>();
            _hubContextMock = new Mock<IHubContext<ChatHub>>();

            _chatGroupRepo = new Mock<IRepository<ChatGroup>>();
            _chatMessageRepo = new Mock<IRepository<ChatMessage>>();
            _chatMessageReadRepo = new Mock<IRepository<ChatMessageRead>>();
            _teamMemberRepo = new Mock<IRepository<TeamMember>>();
            _userRepo = new Mock<IRepository<User>>();

            _uowMock.Setup(u => u.ChatGroups).Returns(_chatGroupRepo.Object);
            _uowMock.Setup(u => u.ChatMessages).Returns(_chatMessageRepo.Object);
            _uowMock.Setup(u => u.ChatMessageReads).Returns(_chatMessageReadRepo.Object);
            _uowMock.Setup(u => u.TeamMembers).Returns(_teamMemberRepo.Object);
            _uowMock.Setup(u => u.Users).Returns(_userRepo.Object);

            _service = new ChatService(_uowMock.Object, _mapperMock.Object, _hubContextMock.Object);
        }

        // =============================
        // 1. SendMessageAsync - Empty content
        // =============================
        [Test]
        public void SendMessageAsync_WhenContentEmpty_Throws()
        {
            var dto = new SendMessageDto
            {
                ChatGroupId = 1,
                Content = ""
            };

            Func<Task> act = async () => await _service.SendMessageAsync(dto, 1);

            act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Message content cannot be empty");
        }

        // =============================
        // 2. SendMessageAsync - Content too long
        // =============================
        [Test]
        public void SendMessageAsync_WhenContentTooLong_Throws()
        {
            var dto = new SendMessageDto
            {
                ChatGroupId = 1,
                Content = new string('a', 5001) // Over 5000 characters
            };

            Func<Task> act = async () => await _service.SendMessageAsync(dto, 1);

            act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Message is too long (max 5000 characters)");
        }

        // =============================
        // 3. SendMessageAsync - User blocked
        // =============================
        [Test]
        public void SendMessageAsync_WhenUserBlocked_Throws()
        {
            var dto = new SendMessageDto
            {
                ChatGroupId = 1,
                Content = "Hello"
            };
            var blockedUser = new User { UserId = 1, IsBlocked = true };

            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(blockedUser);

            Func<Task> act = async () => await _service.SendMessageAsync(dto, 1);

            act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Your account has been blocked");
        }

        // =============================
        // 4. SendMessageAsync - Chat group not found
        // =============================
        [Test]
        public void SendMessageAsync_WhenChatGroupNotFound_Throws()
        {
            var dto = new SendMessageDto
            {
                ChatGroupId = 99,
                Content = "Hello"
            };
            var user = new User { UserId = 1, IsBlocked = false };

            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
            _chatGroupRepo.Setup(r => r.GetByIdIncludingAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<ChatGroup, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<System.Func<ChatGroup, object>>[]>()))
                .ReturnsAsync((ChatGroup)null);

            Func<Task> act = async () => await _service.SendMessageAsync(dto, 1);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Chat Group not found");
        }

        // =============================
        // 5. SendMessageAsync - User not authorized
        // =============================
        [Test]
        public void SendMessageAsync_WhenUserNotAuthorized_Throws()
        {
            var dto = new SendMessageDto
            {
                ChatGroupId = 1,
                Content = "Hello"
            };
            var user = new User { UserId = 1, IsBlocked = false };
            var chatGroup = new ChatGroup { ChatGroupId = 1, MentorId = 2, TeamId = 1 };

            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
            _chatGroupRepo.Setup(r => r.GetByIdIncludingAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<ChatGroup, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<System.Func<ChatGroup, object>>[]>()))
                .ReturnsAsync(chatGroup);
            _chatGroupRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(chatGroup);
            _teamMemberRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamMember, bool>>>()))
                .ReturnsAsync(false);

            Func<Task> act = async () => await _service.SendMessageAsync(dto, 1);

            act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("You are not authorized to send messages in this group.");
        }

       

        // =============================
        // 7. GetMessagesAsync - Return messages for chat group
        // =============================
        [Test]
        public async Task GetMessagesAsync_ShouldReturnMessagesOrderedBySentAt()
        {
            var messages = new List<ChatMessage>
            {
                new ChatMessage 
                { 
                    MessageId = 1, 
                    ChatGroupId = 1, 
                    Content = "First message",
                    SentAt = DateTime.UtcNow.AddMinutes(-2),
                    Sender = new User { UserId = 1 },
                    ChatMessageReads = new List<ChatMessageRead>()
                },
                new ChatMessage 
                { 
                    MessageId = 2, 
                    ChatGroupId = 1, 
                    Content = "Second message",
                    SentAt = DateTime.UtcNow.AddMinutes(-1),
                    Sender = new User { UserId = 2 },
                    ChatMessageReads = new List<ChatMessageRead>()
                }
            };
            var messageDtos = new List<ChatMessageDto>
            {
                new ChatMessageDto { MessageId = 1, Content = "First message" },
                new ChatMessageDto { MessageId = 2, Content = "Second message" }
            };

            _chatMessageRepo.Setup(r => r.GetAllIncludingAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<ChatMessage, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<System.Func<ChatMessage, object>>[]>()))
                .ReturnsAsync(messages);
            _userRepo.Setup(r => r.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<User, bool>>>(), null, null))
                .ReturnsAsync(new List<User>());
            _mapperMock.Setup(m => m.Map<IEnumerable<ChatMessageDto>>(It.IsAny<List<ChatMessage>>()))
                .Returns(messageDtos);

            var result = await _service.GetMessagesAsync(1);

            result.Should().HaveCount(2);
            result.First().Content.Should().Be("First message");
            result.Last().Content.Should().Be("Second message");
        }

        // =============================
        // 8. GetMessagesPaginatedAsync - Return paginated messages
        // =============================
        [Test]
        public async Task GetMessagesPaginatedAsync_ShouldReturnPaginatedResult()
        {
            var messages = new List<ChatMessage>
            {
                new ChatMessage 
                { 
                    MessageId = 1, 
                    ChatGroupId = 1, 
                    Content = "Message 1",
                    SentAt = DateTime.UtcNow.AddMinutes(-3),
                    Sender = new User { UserId = 1 },
                    ChatMessageReads = new List<ChatMessageRead>()
                },
                new ChatMessage 
                { 
                    MessageId = 2, 
                    ChatGroupId = 1, 
                    Content = "Message 2",
                    SentAt = DateTime.UtcNow.AddMinutes(-2),
                    Sender = new User { UserId = 2 },
                    ChatMessageReads = new List<ChatMessageRead>()
                }
            };
            var messageDtos = new List<ChatMessageDto>
            {
                new ChatMessageDto { MessageId = 1, Content = "Message 1" },
                new ChatMessageDto { MessageId = 2, Content = "Message 2" }
            };

            _chatMessageRepo.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<ChatMessage, bool>>>()))
                .ReturnsAsync(2);
            _chatMessageRepo.Setup(r => r.GetAllIncludingAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<ChatMessage, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<System.Func<ChatMessage, object>>[]>()))
                .ReturnsAsync(messages);
            _userRepo.Setup(r => r.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<User, bool>>>(), null, null))
                .ReturnsAsync(new List<User>());
            _mapperMock.Setup(m => m.Map<List<ChatMessageDto>>(It.IsAny<List<ChatMessage>>()))
                .Returns(messageDtos);

            var result = await _service.GetMessagesPaginatedAsync(1, 1, 10);

            result.Should().NotBeNull();
            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
            result.Page.Should().Be(1);
            result.PageSize.Should().Be(10);
        }

        // =============================
        // 9. GetChatGroupsByMentorAsync - Return mentor's chat groups
        // =============================
        [Test]
        public async Task GetChatGroupsByMentorAsync_ShouldReturnMentorChatGroups()
        {
            var chatGroups = new List<ChatGroup>
            {
                new ChatGroup 
                { 
                    ChatGroupId = 1, 
                    MentorId = 1,
                    TeamId = 1,
                    HackathonId = 1,
                    GroupName = "Group 1",
                    CreatedAt = DateTime.UtcNow,
                    Mentor = new User { UserId = 1, FullName = "Mentor Name" },
                    Team = new Team { TeamId = 1, TeamName = "Team Name" },
                    Hackathon = new Hackathon { HackathonId = 1, Name = "Hackathon Name" },
                    ChatMessages = new List<ChatMessage>()
                }
            };

            _chatGroupRepo.Setup(r => r.GetAllIncludingAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<ChatGroup, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<System.Func<ChatGroup, object>>[]>()))
                .ReturnsAsync(chatGroups);

            var result = await _service.GetChatGroupsByMentorAsync(1);

            result.Should().HaveCount(1);
            result.First().MentorId.Should().Be(1);
            result.First().GroupName.Should().Be("Group 1");
        }

        // =============================
        // 10. GetChatGroupsByTeamAsync - Return team's chat groups
        // =============================
        [Test]
        public async Task GetChatGroupsByTeamAsync_ShouldReturnTeamChatGroups()
        {
            var chatGroups = new List<ChatGroup>
            {
                new ChatGroup 
                { 
                    ChatGroupId = 1, 
                    MentorId = 1,
                    TeamId = 1,
                    HackathonId = 1,
                    GroupName = "Group 1",
                    CreatedAt = DateTime.UtcNow,
                    Mentor = new User { UserId = 1, FullName = "Mentor Name" },
                    Team = new Team { TeamId = 1, TeamName = "Team Name" },
                    Hackathon = new Hackathon { HackathonId = 1, Name = "Hackathon Name" },
                    ChatMessages = new List<ChatMessage>()
                }
            };

            _chatGroupRepo.Setup(r => r.GetAllIncludingAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<ChatGroup, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<System.Func<ChatGroup, object>>[]>()))
                .ReturnsAsync(chatGroups);

            var result = await _service.GetChatGroupsByTeamAsync(1);

            result.Should().HaveCount(1);
            result.First().TeamId.Should().Be(1);
            result.First().GroupName.Should().Be("Group 1");
        }

        // =============================
        // 11. MarkAsReadAsync - Chat group not found
        // =============================
        [Test]
        public void MarkAsReadAsync_WhenChatGroupNotFound_Throws()
        {
            _chatGroupRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((ChatGroup)null);

            Func<Task> act = async () => await _service.MarkAsReadAsync(99, 1);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Chat group not found.");
        }

       

        // =============================
        // 13. ValidateUserAccessAsync - User is mentor
        // =============================
        [Test]
        public async Task ValidateUserAccessAsync_WhenUserIsMentor_ReturnsTrue()
        {
            var chatGroup = new ChatGroup { ChatGroupId = 1, MentorId = 1, TeamId = 1 };

            _chatGroupRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(chatGroup);

            var result = await _service.ValidateUserAccessAsync(1, 1);

            result.Should().BeTrue();
        }

        // =============================
        // 14. ValidateUserAccessAsync - User is team member
        // =============================
        [Test]
        public async Task ValidateUserAccessAsync_WhenUserIsTeamMember_ReturnsTrue()
        {
            var chatGroup = new ChatGroup { ChatGroupId = 1, MentorId = 2, TeamId = 1 };

            _chatGroupRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(chatGroup);
            _teamMemberRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamMember, bool>>>()))
                .ReturnsAsync(true);

            var result = await _service.ValidateUserAccessAsync(1, 1);

            result.Should().BeTrue();
        }

        // =============================
        // 15. ValidateUserAccessAsync - User has no access
        // =============================
        [Test]
        public async Task ValidateUserAccessAsync_WhenUserHasNoAccess_ReturnsFalse()
        {
            var chatGroup = new ChatGroup { ChatGroupId = 1, MentorId = 2, TeamId = 1 };

            _chatGroupRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(chatGroup);
            _teamMemberRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamMember, bool>>>()))
                .ReturnsAsync(false);

            var result = await _service.ValidateUserAccessAsync(1, 1);

            result.Should().BeFalse();
        }

        // =============================
        // 16. IsUserBlockedAsync - User is blocked
        // =============================
        [Test]
        public async Task IsUserBlockedAsync_WhenUserIsBlocked_ReturnsTrue()
        {
            var user = new User { UserId = 1, IsBlocked = true };

            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

            var result = await _service.IsUserBlockedAsync(1);

            result.Should().BeTrue();
        }

        // =============================
        // 17. IsUserBlockedAsync - User is not blocked
        // =============================
        [Test]
        public async Task IsUserBlockedAsync_WhenUserIsNotBlocked_ReturnsFalse()
        {
            var user = new User { UserId = 1, IsBlocked = false };

            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

            var result = await _service.IsUserBlockedAsync(1);

            result.Should().BeFalse();
        }
    }
}