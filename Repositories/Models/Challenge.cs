using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Models
{
    public partial class Challenge
    {
        public int ChallengeId { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string? FilePath { get; set; } // PDF/DOC path
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Season
        public int SeasonId { get; set; }
        public virtual Season Season { get; set; } = null!;

        // Partner (User role = Partner)
        public int UserId { get; set; }
        public virtual User User { get; set; } = null!;

      //  public virtual ICollection<TeamChallenge> TeamChallenges { get; set; } = new List<TeamChallenge>();
    }
}
