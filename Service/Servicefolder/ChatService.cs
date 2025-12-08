using AutoMapper;
using Common.DTOs.ChatDto;
using Common.Wrappers;
using Microsoft.AspNetCore.SignalR;
using Repositories.Models;
using Repositories.UnitOfWork;
using Service.Interface;
using Service.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Service.Servicefolder
{
    public class ChatService : IChatService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;
        private readonly IHubContext<ChatHub> _hubContext;
        public ChatService(IUOW uow, IMapper mapper, IHubContext<ChatHub> hubContext)
        {
            _uow = uow;
            _mapper = mapper;
            _hubContext = hubContext;
        }

        public async Task<ChatMessageDto> SendMessageAsync(SendMessageDto dto, int senderId)
        {
            // 0. Validate input
            if (string.IsNullOrWhiteSpace(dto.Content))
                throw new ArgumentException("Message content cannot be empty");

            if (dto.Content.Length > 5000)
                throw new ArgumentException("Message is too long (max 5000 characters)");

            // Check if user is blocked
            if (await IsUserBlockedAsync(senderId))
                throw new UnauthorizedAccessException("Your account has been blocked");

            // 1. Lấy ChatGroup (phải tồn tại rồi vì đã tạo khi approve)
            var chatGroup = await _uow.ChatGroups.GetByIdIncludingAsync(
                cg => cg.ChatGroupId == dto.ChatGroupId,
                cg => cg.Mentor,
                cg => cg.Team,
                cg => cg.Hackathon);

            if (chatGroup == null)
                throw new Exception("Chat Group not found");

            // 2. Kiểm tra sender có quyền gửi message không
            if (!await ValidateUserAccessAsync(dto.ChatGroupId, senderId))
                throw new UnauthorizedAccessException("You are not authorized to send messages in this group.");

            // 3. Cập nhật LastMessageAt
            chatGroup.LastMessageAt = DateTime.UtcNow;
            _uow.ChatGroups.Update(chatGroup);

            // 4. Tạo ChatMessage
            var message = new ChatMessage
            {
                ChatGroupId = dto.ChatGroupId,
                SenderId = senderId,
                Content = dto.Content,
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            await _uow.ChatMessages.AddAsync(message);
            await _uow.SaveAsync();

            // 5. Tạo ChatMessageRead cho sender (người gửi tự động đã đọc)
            var senderRead = new ChatMessageRead
            {
                MessageId = message.MessageId,
                UserId = senderId,
                ReadAt = DateTime.UtcNow
            };

            await _uow.ChatMessageReads.AddAsync(senderRead);
            await _uow.SaveAsync();

            // 6. Reload message với đầy đủ includes để map DTO
            var created = await _uow.ChatMessages.GetByIdIncludingAsync(
                m => m.MessageId == message.MessageId,
                m => m.Sender,
                m => m.ChatMessageReads);

            // Load User cho mỗi ChatMessageRead
            if (created?.ChatMessageReads != null)
            {
                foreach (var read in created.ChatMessageReads)
                {
                    if (read.User == null)
                    {
                        read.User = await _uow.Users.GetByIdAsync(read.UserId);
                    }
                }
            }
            // Map DTO - đảm bảo ReadStatuses được map đúng
            var messageDto = _mapper.Map<ChatMessageDto>(created);

            // Nếu ReadStatuses null, set empty list
            if (messageDto.ReadStatuses == null)
            {
                messageDto.ReadStatuses = new List<MessageReadStatusDto>();
            }

            // 7. Broadcast message qua SignalR đến tất cả user trong chat group
            await _hubContext.Clients.Group($"ChatGroup_{chatGroup.ChatGroupId}")
                .SendAsync("ReceiveMessage", messageDto);

            // 8. Gửi notification đến mentor (nếu sender là team member)
            if (senderId != chatGroup.MentorId)
            {
                // Team member gửi → notify mentor
                await _hubContext.Clients.Group($"User_{chatGroup.MentorId}")
                    .SendAsync("NewMessageNotification", new
                    {
                        ChatGroupId = chatGroup.ChatGroupId,
                        TeamName = chatGroup.Team?.TeamName,
                        Message = dto.Content
                    });
            }

            // 9. Gửi notification đến tất cả team members (nếu sender là mentor)
            else
            {
                // Mentor gửi → notify tất cả team members
                var teamMembers = await _uow.TeamMembers.GetAllAsync(
                    tm => tm.TeamId == chatGroup.TeamId);

                foreach (var member in teamMembers)
                {
                    await _hubContext.Clients.Group($"User_{member.UserId}")
                        .SendAsync("NewMessageNotification", new
                        {
                            ChatGroupId = chatGroup.ChatGroupId,
                            MentorName = chatGroup.Mentor?.FullName,
                            Message = dto.Content
                        });
                }
            }

            return messageDto;
        }

        public async Task<IEnumerable<ChatMessageDto>> GetMessagesAsync(int chatGroupId)
        {
            // Lấy messages với đầy đủ read statuses
            var messages = await _uow.ChatMessages.GetAllIncludingAsync(
                m => m.ChatGroupId == chatGroupId,
                m => m.Sender,
                m => m.ChatMessageReads);

            // Batch load users (OPTIMIZED - single query instead of N queries)
            var userIds = messages
                .SelectMany(m => m.ChatMessageReads?.Select(r => r.UserId) ?? Enumerable.Empty<int>())
                .Distinct()
                .ToList();

            var users = (await _uow.Users.GetAllAsync(u => userIds.Contains(u.UserId)))
                .ToDictionary(u => u.UserId);

            // Gán User vào ChatMessageRead
            foreach (var message in messages)
            {
                if (message.ChatMessageReads != null)
                {
                    foreach (var read in message.ChatMessageReads)
                    {
                        if (users.ContainsKey(read.UserId))
                            read.User = users[read.UserId];
                    }
                }
            }

            var orderedMessages = messages.OrderBy(m => m.SentAt).ToList();
            return _mapper.Map<IEnumerable<ChatMessageDto>>(orderedMessages);
        }

        public async Task<PagedResult<ChatMessageDto>> GetMessagesPaginatedAsync(int chatGroupId, int page = 1, int pageSize = 50)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 50;

            // Get total count
            var totalCount = await _uow.ChatMessages.CountAsync(m => m.ChatGroupId == chatGroupId);

            // Get paginated messages (latest first, then reverse for display)
            var messages = await _uow.ChatMessages.GetAllIncludingAsync(
                m => m.ChatGroupId == chatGroupId,
                m => m.Sender,
                m => m.ChatMessageReads);

            var pagedMessages = messages
                .OrderByDescending(m => m.SentAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .OrderBy(m => m.SentAt) // Reverse for chronological order
                .ToList();

            // Batch load users
            var userIds = pagedMessages
                .SelectMany(m => m.ChatMessageReads?.Select(r => r.UserId) ?? Enumerable.Empty<int>())
                .Distinct()
                .ToList();

            var users = (await _uow.Users.GetAllAsync(u => userIds.Contains(u.UserId)))
                .ToDictionary(u => u.UserId);

            // Assign users to read statuses
            foreach (var message in pagedMessages)
            {
                if (message.ChatMessageReads != null)
                {
                    foreach (var read in message.ChatMessageReads)
                    {
                        if (users.ContainsKey(read.UserId))
                            read.User = users[read.UserId];
                    }
                }
            }

            var messageDtos = _mapper.Map<List<ChatMessageDto>>(pagedMessages);
            return PagedResult<ChatMessageDto>.Create(messageDtos, page, pageSize, totalCount);
        }
        // ========== LẤY CHATGROUPS THEO MENTOR ==========
        // Mentor xem danh sách teams đang chat
        public async Task<IEnumerable<ChatGroupDto>> GetChatGroupsByMentorAsync(int mentorId)
        {
            var groups = await _uow.ChatGroups.GetAllIncludingAsync(
                cg => cg.MentorId == mentorId,
                cg => cg.Mentor,
                cg => cg.Team,
                cg => cg.Hackathon,
                cg => cg.ChatMessages);

            return MapChatGroupsToDtos(groups, null);
        }
        // ========== LẤY CHATGROUPS THEO TEAM ==========
        // Team xem danh sách mentors đang chat
        public async Task<IEnumerable<ChatGroupDto>> GetChatGroupsByTeamAsync(int teamId)
        {
            var groups = await _uow.ChatGroups.GetAllIncludingAsync(
                cg => cg.TeamId == teamId,
                cg => cg.Team,
                cg => cg.Mentor,
                cg => cg.Hackathon,
                cg => cg.ChatMessages);

            return MapChatGroupsToDtos(groups, teamId);
        }

        // ========== LẤY CHATGROUPS THEO HACKATHON ==========
        public async Task<IEnumerable<ChatGroupDto>> GetChatGroupsByHackathonAsync(int hackathonId)
        {
            var groups = await _uow.ChatGroups.GetAllIncludingAsync(
                cg => cg.HackathonId == hackathonId,
                cg => cg.Mentor,
                cg => cg.Team,
                cg => cg.Hackathon,
                cg => cg.ChatMessages);

            return MapChatGroupsToDtos(groups, null);
        }

        // ========== LẤY CHATGROUPS THEO TEAM + HACKATHON ==========
        public async Task<IEnumerable<ChatGroupDto>> GetChatGroupsByTeamAndHackathonAsync(int teamId, int hackathonId)
        {
            var groups = await _uow.ChatGroups.GetAllIncludingAsync(
                cg => cg.TeamId == teamId && cg.HackathonId == hackathonId,
                cg => cg.Mentor,
                cg => cg.Team,
                cg => cg.Hackathon,
                cg => cg.ChatMessages);

            return MapChatGroupsToDtos(groups, teamId);
        }

        // ========== LẤY CHATGROUPS THEO MENTOR + HACKATHON ==========
        public async Task<IEnumerable<ChatGroupDto>> GetChatGroupsByMentorAndHackathonAsync(int mentorId, int hackathonId)
        {
            var groups = await _uow.ChatGroups.GetAllIncludingAsync(
                cg => cg.MentorId == mentorId && cg.HackathonId == hackathonId,
                cg => cg.Mentor,
                cg => cg.Team,
                cg => cg.Hackathon,
                cg => cg.ChatMessages);

            return MapChatGroupsToDtos(groups, null);
        }

        public async Task MarkAsReadAsync(int chatGroupId, int userId)
        {
            var chatGroup = await _uow.ChatGroups.GetByIdAsync(chatGroupId);
            if (chatGroup == null)
                throw new Exception("Chat group not found.");

            // Get user info for broadcast
            var user = await _uow.Users.GetByIdAsync(userId);
            var userName = user?.FullName ?? "Unknown User";

            // Lấy tất cả messages chưa đọc (không phải của user này và chưa có ChatMessageRead)
            var unreadMessages = await _uow.ChatMessages.GetAllIncludingAsync(
                m => m.ChatGroupId == chatGroupId &&
                     m.SenderId != userId &&
                     !m.ChatMessageReads.Any(r => r.UserId == userId),
                m => m.ChatMessageReads);

            var messageIds = new List<int>();
            var readRecords = new List<ChatMessageRead>();

            foreach (var msg in unreadMessages)
            {
                // Tạo ChatMessageRead record
                var readRecord = new ChatMessageRead
                {
                    MessageId = msg.MessageId,
                    UserId = userId,
                    ReadAt = System.DateTime.UtcNow
                };
                readRecords.Add(readRecord);
                messageIds.Add(msg.MessageId);
            }

            // Batch insert tất cả read records
            if (readRecords.Any())
            {
                await _uow.ChatMessageReads.AddRangeAsync(readRecords);
                await _uow.SaveAsync();

                // Broadcast read receipt qua SignalR with UserName
                await _hubContext.Clients.Group($"ChatGroup_{chatGroupId}")
                    .SendAsync("MessageRead", new
                    {
                        ChatGroupId = chatGroupId,
                        UserId = userId,
                        UserName = userName,
                        MessageIds = messageIds,
                        ReadAt = System.DateTime.UtcNow
                    });
            }
        }

        // ========== HELPER METHOD: MAP CHATGROUPS TO DTOS ==========
        private List<ChatGroupDto> MapChatGroupsToDtos(IEnumerable<ChatGroup> groups, int? currentUserId)
        {
            var result = new List<ChatGroupDto>();

            foreach (var group in groups.OrderByDescending(g => g.LastMessageAt ?? g.CreatedAt))
            {
                var lastMessage = group.ChatMessages?.OrderByDescending(m => m.SentAt).FirstOrDefault();

                // Tính unread count
                int unreadCount = 0;
                if (currentUserId.HasValue)
                {
                    // Nếu là team member, đếm messages từ mentor chưa đọc
                    unreadCount = group.ChatMessages?.Count(m =>
                        m.SenderId == group.MentorId &&
                        !m.ChatMessageReads.Any(r => r.UserId == currentUserId.Value)) ?? 0;
                }
                else
                {
                    // Nếu không có userId, không tính unread
                    unreadCount = 0;
                }

                result.Add(new ChatGroupDto
                {
                    ChatGroupId = group.ChatGroupId,
                    MentorId = group.MentorId,
                    MentorName = group.Mentor?.FullName ?? "",
                    TeamId = group.TeamId,
                    TeamName = group.Team?.TeamName ?? "",
                    HackathonId = group.HackathonId,
                    HackathonName = group.Hackathon?.Name ?? "",
                    GroupName = group.GroupName,
                    CreatedAt = group.CreatedAt,
                    LastMessageAt = group.LastMessageAt,
                    LastMessageContent = lastMessage?.Content,
                    UnreadCount = unreadCount
                });
            }

            return result;
        }

        // ========== VALIDATION METHODS ==========
        
        public async Task<bool> ValidateUserAccessAsync(int chatGroupId, int userId)
        {
            var chatGroup = await _uow.ChatGroups.GetByIdAsync(chatGroupId);
            if (chatGroup == null)
                return false;

            // Check if user is mentor
            if (chatGroup.MentorId == userId)
                return true;

            // Check if user is team member
            var isTeamMember = await _uow.TeamMembers.ExistsAsync(
                tm => tm.TeamId == chatGroup.TeamId && tm.UserId == userId);

            return isTeamMember;
        }

        public async Task<bool> IsUserBlockedAsync(int userId)
        {
            var user = await _uow.Users.GetByIdAsync(userId);
            return user?.IsBlocked ?? false;
        }
    }
}
