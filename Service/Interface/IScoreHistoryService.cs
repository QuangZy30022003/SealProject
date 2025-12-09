using Common.DTOs.ScoreDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface IScoreHistoryService
    {
        Task<List<ScoreHistoryDto>> GetHistoryByHackathonAsync(int hackathonId);
        Task<List<ScoreHistoryDto>> GetHistoryBySubmissionAsync(int submissionId);
        Task<List<ScoreHistoryDto>> GetHistoryByJudgeAsync(int judgeId);

    }
}
