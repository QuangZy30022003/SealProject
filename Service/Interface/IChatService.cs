using Common.DTOs.ChatDto;
using Common.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface IChatService
    {
        Task<ChatMessageDto> SendMessageAsync(SendMessageDto dto, int senderId);
        Task<IEnumerable<ChatMessageDto>> GetMessagesAsync(int chatGroupId);
        Task<PagedResult<ChatMessageDto>> GetMessagesPaginatedAsync(int chatGroupId, int page = 1, int pageSize = 50);
        Task<IEnumerable<ChatGroupDto>> GetChatGroupsByMentorAsync(int mentorId);
        Task<IEnumerable<ChatGroupDto>> GetChatGroupsByTeamAsync(int teamId);
        Task MarkAsReadAsync(int chatGroupId, int userId);
        Task<bool> ValidateUserAccessAsync(int chatGroupId, int userId);
        Task<bool> IsUserBlockedAsync(int userId);

        // ✅ MỚI: Lấy ChatGroups theo Hackathon
        Task<IEnumerable<ChatGroupDto>> GetChatGroupsByHackathonAsync(int hackathonId);

        // ✅ MỚI: Lấy ChatGroups theo Team + Hackathon
        Task<IEnumerable<ChatGroupDto>> GetChatGroupsByTeamAndHackathonAsync(int teamId, int hackathonId);

        // ✅ MỚI: Lấy ChatGroups theo Mentor + Hackathon
        Task<IEnumerable<ChatGroupDto>> GetChatGroupsByMentorAndHackathonAsync(int mentorId, int hackathonId);
    }
}
