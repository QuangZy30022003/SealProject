using Common.DTOs.TeamJoinRequestDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface ITeamJoinRequestService
    {
        Task<JoinRequestResponseDto> CreateJoinRequestAsync(CreateJoinRequestDto dto, int userId);
        Task<IEnumerable<JoinRequestResponseDto>> GetJoinRequestsForTeamAsync(int teamId);
        Task<IEnumerable<JoinRequestResponseDto>> GetJoinRequestsByUserAsync(int userId);
        Task<JoinRequestResponseDto?> RespondToJoinRequestAsync(int requestId, RespondToJoinRequestDto dto, int leaderId);
        Task<JoinRequestResponseDto?> GetJoinRequestByIdAsync(int requestId);
    }
}
