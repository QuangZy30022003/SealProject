using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.AssignedTeamDto
{
    public class AssignedTeamDto
    {
        public int AssignmentId { get; set; }
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public DateTime? AssignedAt { get; set; }

        // Thông tin leader
        public int? LeaderId { get; set; }
        public string? LeaderName { get; set; }
        public string Status { get; set; }
    }

    public class MentorAssignmentCreateDto
    {
        public int MentorId { get; set; }
        public int HackathonId { get; set; }
        public int TeamId { get; set; }
    }

    public class MentorAssignmentResponseDto
    {
        public int AssignmentId { get; set; }
        public int MentorId { get; set; }
        public int HackathonId { get; set; }
        public int TeamId { get; set; }
        public DateTime? AssignedAt { get; set; }
        public string Status { get; set; }
    }
}
