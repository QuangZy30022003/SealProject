using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.TeamJoinRequestDto
{
    public class JoinRequestResponseDto
    {
        public int RequestId { get; set; }
        public int TeamId { get; set; }
        public string? TeamName { get; set; }
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public string? UserEmail { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public string? LeaderResponse { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
    }
}
