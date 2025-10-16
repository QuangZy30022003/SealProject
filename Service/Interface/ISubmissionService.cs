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
        Task<SubmissionResponseDto> CreateDraftSubmissionAsync(SubmissionCreateDto dto, int userId);
        Task<SubmissionResponseDto> SetFinalSubmissionAsync(SubmissionSelectFinalDto dto, int userId);
        Task<IEnumerable<SubmissionResponseDto>> GetSubmissionsByTeamAndPhaseAsync(int? teamId, int? phaseId);
    }

}
