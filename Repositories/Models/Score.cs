﻿using System;
using System.Collections.Generic;

namespace Repositories.Models;

public partial class Score
{
    public int ScoreId { get; set; }

    public int? SubmissionId { get; set; }

    public int? JudgeId { get; set; }

    public int? CriteriaId { get; set; }

    public decimal Score1 { get; set; }

    public string? Comment { get; set; }

    public DateTime? ScoredAt { get; set; }

    public virtual Criterion? Criteria { get; set; }

    public virtual User? Judge { get; set; }

    public virtual Submission? Submission { get; set; }
}
