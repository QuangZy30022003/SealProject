using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.RanksDTo
{
    public class RankingDto
    {
        public int RankingId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string HackathonName { get; set; } = string.Empty;
        public decimal TotalScore { get; set; }
        public int? Rank { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
