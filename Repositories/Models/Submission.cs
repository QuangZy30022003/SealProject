using System;
using System.Collections.Generic;

namespace Repositories.Models;

public partial class Submission
{
    public int SubmissionId { get; set; }

    public int TeamId { get; set; }
    public int PhaseId { get; set; }          
    public int SubmittedBy { get; set; }      

    public string? Title { get; set; }
    public string? GitHubLink { get; set; }
    public string? DemoLink { get; set; }
    public string? ReportLink { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public bool IsFinal { get; set; } = false;  

    public virtual Team Team { get; set; } = null!;
    public virtual HackathonPhase HackathonPhase { get; set; } = null!;
    public virtual User SubmittedByUser { get; set; } = null!;
    public virtual ICollection<Score> Scores { get; set; } = new List<Score>();
}
