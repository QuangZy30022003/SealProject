using Common.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.AppealDto
{
    public class AppealResponseDto
    {
        public int AppealId { get; set; }
        public string? AppealType { get; set; }

        public int TeamId { get; set; }
        public string? TeamName { get; set; }

        public int? AdjustmentId { get; set; }
        public string? PenaltyType { get; set; }  // Penalty / Bonus

        public int? ScoreId { get; set; }
        public decimal? ScoreValue { get; set; }

        public string Message { get; set; }
        public string? Reason { get; set; }
        public string Status { get; set; }
        public string? AdminResponse { get; set; }

        public int? ReviewedById { get; set; }
        public string? ReviewedByName { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }
}
