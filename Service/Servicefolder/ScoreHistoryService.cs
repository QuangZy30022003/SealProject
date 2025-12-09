using AutoMapper;
using Common.DTOs.ScoreDto;
using Repositories.UnitOfWork;
using Service.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Servicefolder
{
    public class ScoreHistoryService : IScoreHistoryService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;

        public ScoreHistoryService(IUOW uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<List<ScoreHistoryDto>> GetHistoryByHackathonAsync(int hackathonId)
        {
            var histories = await _uow.ScoreHistorys.GetAllIncludingAsync(
                h => h.Submission.Phase.HackathonId == hackathonId,
                h => h.Criteria,
                h => h.Judge,
                h => h.Submission,
                h => h.Submission.Team,
                h => h.Submission.Phase,
                h => h.Score
            );

            var ordered = histories
                .OrderByDescending(h => h.ChangedAt)
                .ToList();

            return _mapper.Map<List<ScoreHistoryDto>>(ordered);
        }

        public async Task<List<ScoreHistoryDto>> GetHistoryBySubmissionAsync(int submissionId)
        {
            var histories = await _uow.ScoreHistorys.GetAllIncludingAsync(
                h => h.SubmissionId == submissionId,
                h => h.Criteria,
                h => h.Judge,
                h => h.Submission,
                h => h.Submission.Team,
                h => h.Submission.Phase,
                h => h.Score
            );

            var ordered = histories
                .OrderByDescending(h => h.ChangedAt)
                .ToList();

            return _mapper.Map<List<ScoreHistoryDto>>(ordered);
        }
        public async Task<List<ScoreHistoryDto>> GetHistoryByJudgeAsync(int judgeId)
        {
            var histories = await _uow.ScoreHistorys.GetAllIncludingAsync(
                h => h.JudgeId == judgeId,
                h => h.Criteria,
                h => h.Judge,
                h => h.Submission,
                h => h.Submission.Team,
                h => h.Submission.Phase,
                h => h.Score
            );

            var ordered = histories
                .OrderByDescending(h => h.ChangedAt)
                .ToList();

            return _mapper.Map<List<ScoreHistoryDto>>(ordered);
        }

    }

}
