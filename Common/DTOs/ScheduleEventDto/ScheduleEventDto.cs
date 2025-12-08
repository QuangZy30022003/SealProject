using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.ScheduleEventDto
{
    public class ScheduleEventDto
    {
        public int EventId { get; set; }
        public int HackathonId { get; set; }
        public string? HackathonName { get; set; }
        public int? PhaseId { get; set; }
        public string? PhaseName { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? Location { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? Description { get; set; }
    }

    public class ScheduleEventCreateDto
    {
        public int HackathonId { get; set; }
        public int? PhaseId { get; set; }
        public string Name { get; set; } = null!;
        public string? Type { get; set; }
        public string? Location { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? Description { get; set; }
    }

    public class ScheduleEventUpdateDto
    {
        public int? PhaseId { get; set; }
        public string Name { get; set; } = null!;
        public string? Type { get; set; }
        public string? Location { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? Description { get; set; }
    }
}
