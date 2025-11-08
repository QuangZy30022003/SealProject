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
    public class ScoreService : IScoreService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;

        public ScoreService(IUOW uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        // ✅ Lưu điểm của nhiều tiêu chí
        public async Task SubmitJudgeScoreAsync(JudgeScoreDto dto, int judgeId)
        {
            // 🧮 1. Tính tổng điểm của các tiêu chí con
            var totalScore = dto.Details.Sum(d => d.Score);

            // 2️⃣ Kiểm tra xem giám khảo này đã chấm tiêu chí lớn này chưa
            var existing = await _uow.ScoreRepository.GetJudgeScoreAsync(
                dto.SubmissionId, judgeId, dto.CriteriaId
            );

            if (existing != null)
            {
                // ✅ Cập nhật điểm cũ
                existing.Score1 = totalScore;
                existing.Comment = dto.Comment;
                existing.ScoredAt = DateTime.Now;
                 _uow.ScoreRepository.Update(existing);
                await _uow.SaveAsync();
            }
            else
            {
                // ✅ Thêm mới
                var score = new Score
                {
                    SubmissionId = dto.SubmissionId,
                    CriteriaId = dto.CriteriaId,
                    JudgeId = judgeId,
                    Score1 = totalScore,
                    Comment = dto.Comment,
                    ScoredAt = DateTime.Now
                };

                await _uow.ScoreRepository.AddAsync(score);
            }

            // 3️⃣ Lưu thay đổi
            await _uow.SaveAsync();
        }

        // ✅ Lấy điểm trung bình theo tiêu chí
        public async Task<List<AverageScoreDto>> GetAverageScoresAsync(int submissionId)
        {
            var groups = await _uow.ScoreRepository.GetScoresGroupedByCriteriaAsync(submissionId);

            return groups.Select(g => new AverageScoreDto
            {
                CriteriaId = g.Key ,
                AverageScore = Math.Round(g.Average(x => (double)x.Score1), 2),
                JudgeCount = g.Select(x => x.JudgeId).Distinct().Count(),
                Comments = g
                    .Where(x => !string.IsNullOrEmpty(x.Comment))
                    .Select(x => new CommentDto
                    {
                        JudgeId = x.JudgeId,
                        Comment = x.Comment
                    }).ToList()
            }).ToList();
        }

        // ✅ Lấy tất cả điểm chi tiết
        public async Task<List<ScoreReadDto>> GetAllScoresAsync(int submissionId)
        {
            var scores = await _uow.ScoreRepository.GetScoresBySubmissionAsync(submissionId);
            return _mapper.Map<List<ScoreReadDto>>(scores);
        }
    }
}
