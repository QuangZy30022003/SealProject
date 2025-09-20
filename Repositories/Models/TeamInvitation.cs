using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Models
{
    public partial class TeamInvitation
    {
        public Guid InvitationId { get; set; } = Guid.NewGuid(); 

        public int TeamId { get; set; }
        public string InvitedEmail { get; set; } = null!;
        public int InvitedByUserId { get; set; }

        public Guid InvitationCode { get; set; } = Guid.NewGuid();

        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }

        public virtual Team Team { get; set; } = null!;
        public virtual User InvitedByUser { get; set; } = null!;
    }
}
