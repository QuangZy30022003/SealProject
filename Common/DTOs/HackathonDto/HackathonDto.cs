using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.HackathonDto
{
    public class HackathonDto
    {
        public string Name { get; set; } = null!;
        public string? Season { get; set; }
        public string? Theme { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public int? CreatedBy { get; set; }
    }

    public class HackathonResponseDto
    {
        public int HackathonId { get; set; }
        public string Name { get; set; } = null!;
        public string? Season { get; set; }
    //    public string? SeasonName { get; set; } // Name
        public string? Theme { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string Status { get; set; }
    }

    public class HackathonCreateDto
    {
        public string Name { get; set; } = null!;
        public int SeasonId { get; set; }    // nhập ID từ client
        public string? Theme { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string Status { get; set; } = "Pending";
    }

    public class StatusUpdateDto
    {
        public string Status { get; set; } = null!;
    }
}
