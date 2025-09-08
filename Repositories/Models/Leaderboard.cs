using System;
using System.Collections.Generic;

namespace Repositories.Models;

public partial class Leaderboard
{
    public int TeamId { get; set; }

    public string TeamName { get; set; } = null!;

    public int HackathonId { get; set; }

    public decimal? TotalScore { get; set; }
}
