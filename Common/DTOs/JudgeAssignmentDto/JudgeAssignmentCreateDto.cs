using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.JudgeAssignmentDto
{
    public class JudgeAssignmentCreateDto
    {
        public int JudgeId { get; set; }
        public int HackathonId { get; set; }
        public int? PhaseId { get; set; }
        public int? TrackId { get; set; }
    }

    public class JudgeAssignmentResponseDto
    {
        public int AssignmentId { get; set; }
        public int JudgeId { get; set; }
        public string JudgeName { get; set; }
        public int HackathonId { get; set; }
        public string HackathonName { get; set; }
        public int? TrackId { get; set; }
        public string TrackName { get; set; }
        public DateTime AssignedAt { get; set; }
        public string Status { get; set; }
    }
}
