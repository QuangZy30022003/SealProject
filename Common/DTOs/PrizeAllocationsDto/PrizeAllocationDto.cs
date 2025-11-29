using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.PrizeAllocationsDto
{
    public class PrizeAllocationResultDto
    {
        public string PrizeName { get; set; }
        public string Reward { get; set; }
        public string TeamName { get; set; }
        public string LeaderName { get; set; }
        public int? Rank { get; set; } // từ bảng Ranking
    }




}
