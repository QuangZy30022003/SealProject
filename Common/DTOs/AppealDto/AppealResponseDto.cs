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
        public string AppealType { get; set; } = string.Empty;
        public int? AdjustmentId { get; set; }
        public int? ScoreId { get; set; }
        public int TeamId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public string? AdminResponse { get; set; }
        public string? ReviewedByName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }
}
