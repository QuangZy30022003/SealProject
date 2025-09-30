﻿using System;
using System.Collections.Generic;

namespace Repositories.Models;

public partial class Hackathon
{
    public int HackathonId { get; set; }

    public string Name { get; set; } = null!;

    public string? Season { get; set; }

    public string? Theme { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public int? CreatedBy { get; set; }
    public virtual Season? SeasonNavigation { get; set; }

    public virtual User? CreatedByNavigation { get; set; }

    public virtual ICollection<Criterion> Criteria { get; set; } = new List<Criterion>();

    public virtual ICollection<HackathonPhase> HackathonPhases { get; set; } = new List<HackathonPhase>();

    public virtual ICollection<PenaltiesBonuse> PenaltiesBonuses { get; set; } = new List<PenaltiesBonuse>();

    public virtual ICollection<Prize> Prizes { get; set; } = new List<Prize>();

    public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();
    public virtual ICollection<TeamChallenge> TeamChallenges { get; set; } = new List<TeamChallenge>();
}
