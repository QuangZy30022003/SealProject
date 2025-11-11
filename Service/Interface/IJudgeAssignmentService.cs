using Common.DTOs.JudgeAssignmentDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface IJudgeAssignmentService
    {
        Task<JudgeAssignmentResponseDto> AssignJudgeAsync(JudgeAssignmentCreateDto dto, int adminId);
        Task<List<JudgeAssignmentResponseDto>> GetByHackathonAsync(int hackathonId);
        Task<bool> RemoveAssignmentAsync(int assignmentId);
        Task<bool> ReactivateAssignmentAsync(int assignmentId);
        }
}
