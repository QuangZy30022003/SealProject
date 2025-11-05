using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.Submission
{
    public class SubmissionCreateDto
    {
        public int TeamId { get; set; }
        public int PhaseChallengeId { get; set; }
        public string? Title { get; set; }
        public string FilePath { get; set; }

    }

    public class SubmissionResponseDto
    {
        public int SubmissionId { get; set; }
        public string TeamName { get; set; } = null!;
        public string PhaseName { get; set; } = null!;
        public int PhaseChallengeId { get; set; }  // 🆕 thêm dòng này
        public string? Title { get; set; }
        public string FilePath { get; set; }

        public bool IsFinal { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public string SubmittedByName { get; set; } = null!;
    }

    // Dùng cho team leader chọn bài final
    public class SubmissionSelectFinalDto
    {
        public int SubmissionId { get; set; }
    }
}
