using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.PenaltyBonusDto
{
    public class UpdatePenaltiesBonuseDto
    {
        public decimal Points { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
