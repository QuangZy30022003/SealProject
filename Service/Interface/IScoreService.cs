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
        Task UpdateAverageAndRankAsync(int submissionId);


        Task<List<TeamScoreDto>> GetTeamScoresByGroupAsync(int groupId);
        Task<List<SubmissionScoresGroupedDto>> GetMyScoresGroupedBySubmissionAsync(int judgeId, int phaseId);

        Task<SubmissionScoresResponseDto> ScoreSubmissionAsync(int judgeId, ScoreSubmissionRequestDto request);
        Task<ScoreDetailDto> UpdateScoreByIdAsync(
    int judgeId,
    int scoreId,
    ScoreUpdateByIdDto request);
        Task<TeamOverviewDto> GetTeamOverviewAsync(
    int teamId,
    int phaseId);
        Task UpdateFinalRankingAsync(Submission submission, int hackathonId);
    }
}
