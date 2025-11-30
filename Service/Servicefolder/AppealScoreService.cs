using AutoMapper;
using Common.DTOs.ScoreDto;
using Repositories.Models;
using Repositories.UnitOfWork;
using Service.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Servicefolder
{
    public class AppealScoreService : IAppealScoreService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;

        public AppealScoreService(IUOW uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<SubmissionScoresResponseDto> ReScoreAppealAsync(int appealId, FinalScoreRequestDto request, int userId)
        {
            var appeal = await _uow.Appeals.GetByIdAsync(appealId);
            if (appeal == null)
                throw new Exception("Appeal not found.");

            if (appeal.Status != "Approved" || appeal.AppealType != "Score")
                throw new Exception("Only approved score appeals can be re-scored.");

            if (appeal.JudgeId != userId)
                throw new Exception("You are not authorized to re-score this appeal.");

            var submission = await _uow.Submissions.GetByIdAsync(appeal.SubmissionId);
            if (submission == null)
                throw new Exception("Submission not found.");

            var result = new SubmissionScoresResponseDto { SubmissionId = submission.SubmissionId };

            foreach (var dto in request.CriteriaScores)
            {
                var existing = await _uow.Scores.FirstOrDefaultAsync(s =>
                    s.SubmissionId == submission.SubmissionId &&
                    s.JudgeId == userId &&
                    s.CriteriaId == dto.CriterionId);

                if (existing == null)
                    throw new Exception($"No existing score found for CriteriaId {dto.CriterionId}.");

                // Lưu lịch sử điểm cũ
                var history = new ScoreHistory
                {
                    ScoreId = existing.ScoreId,
                    SubmissionId = existing.SubmissionId,
                    JudgeId = existing.JudgeId,
                    CriteriaId = existing.CriteriaId,
                    OldScore = (int)existing.Score1,
                    OldComment = existing.Comment,
                    ChangedAt = DateTime.UtcNow,
                    ChangeReason = "Re-scored via approved appeal",
                    ChangedBy = userId
                };
                await _uow.ScoreHistorys.AddAsync(history);

                // Cập nhật score mới
                existing.Score1 = dto.Score;
                existing.Comment = dto.Comment;
                existing.ScoredAt = DateTime.UtcNow;
                _uow.Scores.Update(existing);

                // Dùng AutoMapper để map dto sang ScoreItemDto
                var scoreItem = _mapper.Map<ScoreItemDto>(dto);
                result.Scores.Add(scoreItem);
            }

            await _uow.SaveAsync();
            await UpdateAverageAndRankAsync(submission.SubmissionId);

            return result;
        }


        private async Task UpdateAverageAndRankAsync(int submissionId)
        {
            var scoreService = new ScoreService(_uow, _mapper);
            await scoreService.UpdateAverageAndRankAsync(submissionId);
        }
    }
}
