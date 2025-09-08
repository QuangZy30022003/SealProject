﻿using System;
using System.Collections.Generic;

namespace Repositories.Models;

public partial class Prize
{
    public int PrizeId { get; set; }

    public int? HackathonId { get; set; }

    public string? PrizeName { get; set; }

    public string? PrizeType { get; set; }

    public int? Rank { get; set; }

    public string? Reward { get; set; }

    public virtual Hackathon? Hackathon { get; set; }

    public virtual ICollection<PrizeAllocation> PrizeAllocations { get; set; } = new List<PrizeAllocation>();
}
