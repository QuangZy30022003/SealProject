using Common.DTOs.ScoreDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface IScoreService
    {
        Task SubmitJudgeScoreAsync(JudgeScoreDto dto, int judgeId);
        Task<List<AverageScoreDto>> GetAverageScoresAsync(int submissionId);
        Task<List<ScoreReadDto>> GetAllScoresAsync(int submissionId);   }
}
