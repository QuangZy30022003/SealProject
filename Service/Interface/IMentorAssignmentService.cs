using Common.DTOs.AssignedTeamDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface IMentorAssignmentService
    {
        Task<IEnumerable<AssignedTeamDto>> ViewAssignedTeamsAsync(int mentorId);

        Task<MentorAssignmentResponseDto> RegisterAsync(MentorAssignmentCreateDto dto);
        Task<MentorAssignmentResponseDto> ApproveAsync(int assignmentId);
        Task<MentorAssignmentResponseDto> RejectAsync(int assignmentId);
        Task<IEnumerable<MentorAssignmentResponseDto>> GetByMentorAsync(int mentorId);
    }
}
