using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.TeamInvitationDto
{
    public class InvitationStatusDto
    {
        public Guid InvitationCode { get; set; }
        public int TeamId { get; set; }
        public string? TeamName { get; set; }
        public string InvitedEmail { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; 
        public DateTime ExpiresAt { get; set; }
        public bool IsExpired { get; set; }
    }
}
