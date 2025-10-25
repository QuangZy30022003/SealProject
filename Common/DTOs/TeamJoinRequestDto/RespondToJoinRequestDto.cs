using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.TeamJoinRequestDto
{
    public class RespondToJoinRequestDto
    {
        public string Status { get; set; } = string.Empty; // Approved | Rejected
        public string? LeaderResponse { get; set; }
    }
}
