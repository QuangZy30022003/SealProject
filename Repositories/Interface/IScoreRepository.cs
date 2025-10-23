using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Interface
{
    public interface IScoreRepository : IRepository<Score>
    {
        Task<Score?> GetJudgeScoreAsync(int submissionId, int judgeId, int criteriaId);
        Task<List<Score>> GetScoresBySubmissionAsync(int submissionId);
        Task<List<IGrouping<int?, Score>>> GetScoresGroupedByCriteriaAsync(int submissionId);
    }
}
