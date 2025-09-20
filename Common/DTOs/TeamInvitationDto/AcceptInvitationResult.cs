using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.TeamInvitationDto
{
    public class AcceptInvitationResult
    {
        public string Message { get; set; } = string.Empty;
        public bool TeamCreated { get; set; }
    }
}
