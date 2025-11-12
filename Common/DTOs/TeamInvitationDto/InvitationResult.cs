using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.TeamInvitationDto
{
    public class InvitationResult
    {
        public string Status { get; set; } = "Failed";
        public string Message { get; set; } = string.Empty;
        public int? TeamId { get; set; }
        public string? TeamName { get; set; }
    }
}
