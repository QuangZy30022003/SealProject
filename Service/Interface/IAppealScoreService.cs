using Common.DTOs.ScoreDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface IAppealScoreService
    {
        Task<SubmissionScoresResponseDto> ReScoreAppealAsync(int appealId, FinalScoreRequestDto request, int userId);
    }
}
