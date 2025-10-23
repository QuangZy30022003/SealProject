using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Models
{
    public partial class TeamJoinRequest
    {
        public int RequestId { get; set; }
        
        public int TeamId { get; set; }
        public int UserId { get; set; } // Người xin vào team
        
        public string Message { get; set; } = string.Empty; // Lời nhắn từ người xin
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public string Status { get; set; } = "Pending"; // Pending | Approved | Rejected
        public string? LeaderResponse { get; set; } // Phản hồi từ leader
        public DateTime? RespondedAt { get; set; }
        
        // Relationships
        public virtual Team Team { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}
