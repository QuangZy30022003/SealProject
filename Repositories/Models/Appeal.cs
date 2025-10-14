using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Models
{
    public partial class Appeal
    {
        public int AppealId { get; set; }

        public int AdjustmentId { get; set; } // FK -> PenaltiesBonuse
        public int TeamId { get; set; }

        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string Status { get; set; } = "Pending"; // Pending | Approved | Rejected
        public string? AdminResponse { get; set; }

        // ✅ thay vì string -> ID của người duyệt
        public int? ReviewedById { get; set; }
        public DateTime? ReviewedAt { get; set; }

        // relationships
        public virtual PenaltiesBonuse? Penalty { get; set; }
        public virtual Team? Team { get; set; }

        // ✅ navigation property
        public virtual User? ReviewedBy { get; set; }
    }
}
