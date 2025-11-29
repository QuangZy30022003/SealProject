using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.AppealDto
{
    public class CreateAppealDto
    {
        public string AppealType { get; set; } = "Penalty"; // Penalty | Score
        public int? AdjustmentId { get; set; } // Cho penalty
        public int? SubmissionId { get; set; } // Cho score
        public int? JudgeId { get; set; }
        public int TeamId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Reason { get; set; } = string.Empty;
    }
}
