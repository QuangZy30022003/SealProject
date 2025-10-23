using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.ScoreDto
{
    public class JudgeScoreDetailDto
    {
        public int CriterionDetailId { get; set; } // tiêu chí con
        public decimal Score { get; set; }         // điểm chấm cho tiêu chí con
    }

    public class JudgeScoreDto
    {
        public int SubmissionId { get; set; }      // bài nộp
        public int CriteriaId { get; set; }        // tiêu chí lớn
        public string? Comment { get; set; }       // nhận xét chung
        public List<JudgeScoreDetailDto> Details { get; set; } = new(); // danh sách tiêu chí con
    }

    public class AverageScoreDto
    {
        public int CriteriaId { get; set; }
        public double AverageScore { get; set; }
        public int JudgeCount { get; set; }
        public List<CommentDto> Comments { get; set; } = new();
    }

    public class CommentDto
    {
        public int JudgeId { get; set; }
        public string? Comment { get; set; }
    }

    public class ScoreReadDto
    {
        public int ScoreId { get; set; }
        public int SubmissionId { get; set; }
        public int CriteriaId { get; set; }
        public string? CriteriaName { get; set; }
        public decimal Score { get; set; }
        public string? Comment { get; set; }
        public int JudgeId { get; set; }
        public string? JudgeName { get; set; }
        public DateTime? ScoredAt { get; set; }
    }
}
