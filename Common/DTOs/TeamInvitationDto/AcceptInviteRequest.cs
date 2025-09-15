using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.TeamInvitationDto
{
    public class AcceptInviteRequest
    {
        public Guid InvitationCode { get; set; }
    }
}
