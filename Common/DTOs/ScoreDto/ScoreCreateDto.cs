using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.ScoreDto
{
    public class ScoreCreateDto
    {
        public int SubmissionId { get; set; }
        public int CriteriaId { get; set; }
        public decimal ScoreValue { get; set; }
        public string? Comment { get; set; }
    }

    public class ScoreResponseDto
    {
        public int ScoreId { get; set; }
        public string SubmissionName { get; set; }
        public string CriteriaName { get; set; }
        public decimal ScoreValue { get; set; }
        public string Comment { get; set; }
        public DateTime ScoredAt { get; set; }
    }

    public class SubmissionScoresResponseDto
    {
        public int SubmissionId { get; set; }
        public List<ScoreItemDto> Scores { get; set; } = new();
    }

    public class ScoreItemDto
    {
        public int CriteriaId { get; set; }
        public decimal ScoreValue { get; set; }
        public string? Comment { get; set; }
    }

    public class SubmissionScoreInputDto
    {
        public int SubmissionId { get; set; }
        public List<ScoreItemDto> Scores { get; set; } = new();
    }

    public class ScoreDetailDto
    {
        public int ScoreId { get; set; }
        public int SubmissionId { get; set; }
        public int CriteriaId { get; set; }
        public string CriteriaName { get; set; }
        public decimal ScoreValue { get; set; }
        public string? Comment { get; set; }
        public DateTime ScoredAt { get; set; }
    }


    public class ScoreWithAverageDto
    {
        public int ScoreId { get; set; }
        public int SubmissionId { get; set; }
        public string CriteriaName { get; set; }
        public decimal ScoreValue { get; set; }
        public string Comment { get; set; }
        public DateTime ScoredAt { get; set; }

        // Điểm trung bình của team
        public decimal TeamAverageScore { get; set; }
    }
    public class TeamScoreDto
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; } // Nếu muốn lấy tên team
        public decimal AverageScore { get; set; }
        public int Rank { get; set; }
    }
    public class SubmissionScoresGroupedDto
    {
        public int SubmissionId { get; set; }
        public string SubmissionName { get; set; }
        public decimal TotalScore { get; set; } // thay đổi tên cho rõ ràng
        public List<ScoreResponseDto> Scores { get; set; } = new List<ScoreResponseDto>();
    }
    public class FinalScoreRequestDto
    {
        public int SubmissionId { get; set; }
        public List<CriterionScoreDto> CriteriaScores { get; set; }
    }

    public class CriterionScoreDto
    {
        public int CriterionId { get; set; }
        public decimal Score { get; set; }
        public string? Comment { get; set; } // thêm comment
    }
    public class FinalScoreResponseDto
    {
        public int SubmissionId { get; set; }
        public int TeamId { get; set; }
        public int JudgeId { get; set; }
        public decimal TotalScore { get; set; }
        public int? Rank { get; set; }
    }
    public class ScoreSubmissionRequestDto
    {
        public int SubmissionId { get; set; }
        public List<CriterionScoreDto> CriteriaScores { get; set; }
    }
    public class ScoreUpdateByIdDto
    {
        public decimal ScoreValue { get; set; }
        public string? Comment { get; set; }
    }

    public class TeamScoreOverviewDto
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; }
        public int PhaseId { get; set; }

        public decimal? AverageScore { get; set; }
        public int? Rank { get; set; }

        public List<SubmissionScoreDetailDto> Submissions { get; set; } = new();
    }

    public class SubmissionScoreDetailDto
    {
        public int SubmissionId { get; set; }
        public string SubmissionTitle { get; set; }
        public decimal TotalScore { get; set; }

        public List<ScoreResponseDto> Scores { get; set; } = new();
    }
    public class TeamOverviewDto
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; }
        public int PhaseId { get; set; }
        public decimal? AverageScore { get; set; }
        public int? Rank { get; set; }

        public List<CriterionScoreDto> CriteriaScores { get; set; } = new();
    }

}
