using Common.DTOs.ScoreDto;
using Common.DTOs.Submission;
using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface IScoreService
    {
        Task<List<ScoreResponseDto>> GetScoresByJudgeAsync(int judgeId, int phaseId);
        Task<SubmissionScoresResponseDto> CreateOrUpdateScoresAsync(int judgeId, List<ScoreCreateDto> dtos);
        Task<List<Submission>> GetSubmissionsForJudgeAsync(int judgeId, int phaseId);
        Task UpdateAverageAndRankAsync(int submissionId);
        Task<SubmissionScoresResponseDto> UpdateScoresByCriteriaAsync(int judgeId, List<ScoreCreateDto> scores);

        Task<List<ScoreWithAverageDto>> GetScoresWithTeamAverageBySubmissionAsync(int submissionId);

        Task<List<TeamScoreDto>> GetTeamScoresByGroupAsync(int groupId);
        Task<List<SubmissionScoresGroupedDto>> GetMyScoresGroupedBySubmissionAsync(int judgeId, int phaseId);
    }
}
