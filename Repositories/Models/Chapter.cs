using System;
using System.Collections.Generic;

namespace Repositories.Models;

public partial class Chapter
{
    public int ChapterId { get; set; }

    public string ChapterName { get; set; } = null!;

    public string? Description { get; set; }

    public int? LeaderId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? Leader { get; set; }

    public virtual ICollection<MentorAssignment> MentorAssignments { get; set; } = new List<MentorAssignment>();

    public virtual ICollection<Team> Teams { get; set; } = new List<Team>();
}
