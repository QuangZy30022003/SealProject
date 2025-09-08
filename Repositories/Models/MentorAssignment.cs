using System;
using System.Collections.Generic;

namespace Repositories.Models;

public partial class MentorAssignment
{
    public int AssignmentId { get; set; }

    public int? MentorId { get; set; }

    public int? ChapterId { get; set; }

    public int? TeamId { get; set; }

    public DateTime? AssignedAt { get; set; }

    public virtual Chapter? Chapter { get; set; }

    public virtual User? Mentor { get; set; }

    public virtual Team? Team { get; set; }
}
