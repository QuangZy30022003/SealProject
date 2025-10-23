using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.HackathonDto
{
    public class HackathonFullCreateDto
    {
        public HackathonCreatePayloadDto Hackathon { get; set; } = null!;
        public List<PhaseCreatePayloadDto>? Phases { get; set; }
        public List<PrizeCreatePayloadDto>? Prizes { get; set; }
        //   public List<ChallengeCreateUnifiedPayloadDto>? Challenges { get; set; }
        public bool AutoAssignChallenges { get; set; } = false;
        public int ChallengesPerPhase { get; set; } = 1;

    }

    public class HackathonCreatePayloadDto
    {
        public string Name { get; set; } = null!;
        public int SeasonId { get; set; }
        public string Theme { get; set; } = null!;
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
    }

    public class PhaseCreatePayloadDto
    {
        public string PhaseName { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class PrizeCreatePayloadDto
    {
        public string PrizeName { get; set; } = null!;
        public string? PrizeType { get; set; }
        public int Rank { get; set; }
        public string? Reward { get; set; }
    }

    //public class ChallengeCreateUnifiedPayloadDto
    //{
    //    public string Title { get; set; } = null!;
    //    public string? Description { get; set; }
    //    public string? FilePath { get; set; }
    //    public int SeasonId { get; set; }
    //}
}
