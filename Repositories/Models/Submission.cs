using System;
using System.Collections.Generic;

namespace Repositories.Models;

public partial class Submission
{
    public int SubmissionId { get; set; }

    public int? TeamId { get; set; }

    public int? HackathonId { get; set; }

    public string? Title { get; set; }

    public string? GitHubLink { get; set; }

    public string? DemoLink { get; set; }

    public string? ReportLink { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public virtual Hackathon? Hackathon { get; set; }

    public virtual ICollection<Score> Scores { get; set; } = new List<Score>();

    public virtual Team? Team { get; set; }
}
