using System;
using System.Collections.Generic;

namespace Repositories.Models;

public partial class Criterion
{
    public int CriteriaId { get; set; }

    public int PhaseChallengeId { get; set; }
    public string Name { get; set; } = null!;

    public decimal Weight { get; set; }

    public virtual PhaseChallenge PhaseChallenge { get; set; } = null!;
    public virtual ICollection<Score> Scores { get; set; } = new List<Score>();
    public virtual ICollection<CriterionDetail> CriterionDetails { get; set; } = new List<CriterionDetail>();

}
