using System;
using System.Collections.Generic;

namespace Repositories.Models;

public partial class Criterion
{
    public int CriteriaId { get; set; }

    public int? HackathonId { get; set; }

    public string Name { get; set; } = null!;

    public decimal Weight { get; set; }

    public virtual Hackathon? Hackathon { get; set; }

    public virtual ICollection<Score> Scores { get; set; } = new List<Score>();
}
