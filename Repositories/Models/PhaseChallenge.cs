using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Models
{
    public partial class PhaseChallenge
    {
        public int PhaseChallengeId { get; set; }
        public int PhaseId { get; set; }
        public int ChallengeId { get; set; }
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        public virtual HackathonPhase Phase { get; set; } = null!;
        public virtual Challenge Challenge { get; set; } = null!;
 
    }

}
