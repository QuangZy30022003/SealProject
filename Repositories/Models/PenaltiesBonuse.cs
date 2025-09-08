using System;
using System.Collections.Generic;

namespace Repositories.Models;

public partial class PenaltiesBonuse
{
    public int AdjustmentId { get; set; }

    public int? TeamId { get; set; }

    public int? HackathonId { get; set; }

    public string? Type { get; set; }

    public decimal? Points { get; set; }

    public string? Reason { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Hackathon? Hackathon { get; set; }

    public virtual Team? Team { get; set; }
}
