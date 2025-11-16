using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.PenaltyBonusDto
{
    public class CreatePenaltiesBonuseDto
    {
        public int TeamId { get; set; }
        public int PhaseId { get; set; }
        public string Type { get; set; } = "Penalty"; // Penalty | Bonus
        public decimal Points { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
