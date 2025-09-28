using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.HackathonPhaseDto
{
    public class HackathonPhaseDto
    {
        public int PhaseId { get; set; }
        public int? HackathonId { get; set; }
        public string PhaseName { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class HackathonPhaseCreateDto
    {
        public int HackathonId { get; set; }
        public string PhaseName { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class HackathonPhaseUpdateDto
    {
         public string PhaseName { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
