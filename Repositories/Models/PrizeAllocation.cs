﻿using System;
using System.Collections.Generic;

namespace Repositories.Models;

public partial class PrizeAllocation
{
    public int AllocationId { get; set; }

    public int? PrizeId { get; set; }

    public int? TeamId { get; set; }

    public int? UserId { get; set; }

    public DateTime? AwardedAt { get; set; }

    public virtual Prize? Prize { get; set; }

    public virtual Team? Team { get; set; }

    public virtual User? User { get; set; }
}
