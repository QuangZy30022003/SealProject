using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Models
{
    public partial class TeamChallenge
    {
        public int TeamId { get; set; }
        public int ChallengeId { get; set; }
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        public virtual Team Team { get; set; } = null!;
        public virtual Challenge Challenge { get; set; } = null!;
    }
}
