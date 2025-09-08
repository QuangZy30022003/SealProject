using System;
using System.Collections.Generic;

namespace Repositories.Models;

public partial class HackathonPhase
{
    public int PhaseId { get; set; }

    public int? HackathonId { get; set; }

    public string PhaseName { get; set; } = null!;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public virtual Hackathon? Hackathon { get; set; }
}
