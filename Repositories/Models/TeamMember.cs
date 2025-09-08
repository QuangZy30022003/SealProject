﻿using System;
using System.Collections.Generic;

namespace Repositories.Models;

public partial class TeamMember
{
    public int TeamId { get; set; }

    public int UserId { get; set; }

    public string? RoleInTeam { get; set; }

    public virtual Team Team { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
