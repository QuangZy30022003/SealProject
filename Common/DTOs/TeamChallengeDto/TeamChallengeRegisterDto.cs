using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.TeamChallengeDto
{
    public class TeamChallengeRegisterDto
    {
        public int TeamId { get; set; }
        public int HackathonId { get; set; }
    }
    public class TeamChallengeResponseDto
    {
        public int TeamChallengeId { get; set; }
        public int TeamId { get; set; }
        public string TeamName { get; set; } = null!;
        public int HackathonId { get; set; }
        public string HackathonName { get; set; } = null!;
        public int PhaseId { get; set; }
        public DateTime RegisteredAt { get; set; }
    }

}
