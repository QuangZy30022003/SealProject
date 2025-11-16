using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.PenaltyBonusDto
{
    public class PenaltiesBonuseResponseDto
    {
        public int AdjustmentId { get; set; }
        public int? TeamId { get; set; }
        public int? PhaseId { get; set; }
        public string Type { get; set; } = string.Empty;
        public decimal? Points { get; set; }
        public string? Reason { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int CreatedBy { get; set; }

        public string? TeamName { get; set; }
        public bool IsDeleted { get; set; }
    }
}
