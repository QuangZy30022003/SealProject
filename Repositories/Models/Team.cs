using System;
using System.Collections.Generic;

namespace Repositories.Models;

public partial class Team
{
    public int TeamId { get; set; }

    public string TeamName { get; set; } = null!;

    public int? ChapterId { get; set; }

    public int? LeaderId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Chapter? Chapter { get; set; }

    public virtual User? Leader { get; set; }

    public virtual ICollection<MentorAssignment> MentorAssignments { get; set; } = new List<MentorAssignment>();

    public virtual ICollection<PenaltiesBonuse> PenaltiesBonuses { get; set; } = new List<PenaltiesBonuse>();

    public virtual ICollection<PrizeAllocation> PrizeAllocations { get; set; } = new List<PrizeAllocation>();

    public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();

    public virtual ICollection<TeamMember> TeamMembers { get; set; } = new List<TeamMember>();

    public virtual ICollection<TeamChallenge> TeamChallenges { get; set; } = new List<TeamChallenge>();

}
