using Microsoft.EntityFrameworkCore;
using Repositories.Interface;
using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Repos
{
    public class ScoreRepository : GenericRepository<Score>, IScoreRepository
    {
        private readonly DbContext _context;
        private readonly DbSet<Score> _dbSet;

        public ScoreRepository(DbContext context) : base(context)
        {
            _context = context;
            _dbSet = context.Set<Score>();
        }

        // ✅ Lấy điểm chấm của 1 giám khảo cho 1 tiêu chí cụ thể

        public async Task<Score?> GetJudgeScoreAsync(int submissionId, int judgeId, int criteriaId)
        {
            return await _dbSet.FirstOrDefaultAsync(s =>
                s.SubmissionId == submissionId &&
                s.JudgeId == judgeId &&
                s.CriteriaId == criteriaId);
        }


        public async Task<List<Score>> GetScoresBySubmissionAsync(int submissionId)
        {
            return await _dbSet
                .Include(s => s.Criteria)
                    .ThenInclude(c => c.CriterionDetails) // ✅ lấy tiêu chí con qua Criterion
                .Include(s => s.Judge)
                .Where(s => s.SubmissionId == submissionId)
                .ToListAsync();
        }

        public async Task<List<IGrouping<int, Score>>> GetScoresGroupedByCriteriaAsync(int submissionId)
        {
            var scores = await _dbSet
                .Where(s => s.SubmissionId == submissionId)
                .ToListAsync();

            return scores.GroupBy(s => s.CriteriaId).ToList();
        }
    }
}
