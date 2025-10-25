using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.PrizeDto
{
    public class PrizeDTO
    {
        public int PrizeId { get; set; }
        public string? PrizeName { get; set; }
        public string? PrizeType { get; set; }
        public int? Rank { get; set; }
        public string? Reward { get; set; }
        public int? HackathonId { get; set; }
        public string? HackathonName { get; set; }
    }

    public class CreatePrizeDTO
    {
        public string PrizeName { get; set; }
        public string? PrizeType { get; set; }
        public int? Rank { get; set; }
        public string? Reward { get; set; }
        public int HackathonId { get; set; }
    }

    public class UpdatePrizeDTO
    {
        public int PrizeId { get; set; }
        public string PrizeName { get; set; }
        public string? PrizeType { get; set; }
        public int? Rank { get; set; }
        public string? Reward { get; set; }
    }
}
