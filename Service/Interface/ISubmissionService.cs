using Common.DTOs.Submission;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface ISubmissionService
    {
        Task<List<SubmissionResponseDto>> GetSubmissionsByTeamAsync(int teamId);
        Task<SubmissionResponseDto?> SetFinalAsync(SubmissionFinalDto dto, int currentUserId);
        Task<SubmissionResponseDto?> UpdateDraftAsync(int submissionId, SubmissionUpdateDto dto, int currentUserId);
        Task<SubmissionResponseDto> CreateDraftAsync(SubmissionCreateDto dto, int currentUserId);
        Task<List<SubmissionResponseDto>> GetFinalSubmissionsByPhaseAsync(int phaseId, int userId, string role);

        Task<SubmissionResponseDto> GetSubmissionByIdAsync(int submissionId);
        Task<List<SubmissionResponseDto>> GetAllSubmissionsAsync();
    }

}
