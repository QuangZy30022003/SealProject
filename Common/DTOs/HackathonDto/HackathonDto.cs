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
        public string? Description { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public int? CreatedBy { get; set; }
    }

    public class HackathonResponseDto
    {
        public int HackathonId { get; set; }
        public string Name { get; set; } = null!;
        public int? SeasonId { get; set; }           // mới: trả về ID để client dùng
        public string? SeasonName { get; set; }      // hiển thị tên Season (ex: "Summer 2025")
        public string? Description { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string Status { get; set; }
    }

    public class HackathonCreateDto
    {
        public string Name { get; set; } = null!;
        public int SeasonId { get; set; }    // nhập ID từ client
        public string? Description { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string Status { get; set; } = "Pending";
    }

    public class StatusUpdateDto
    {
        public string Status { get; set; } = null!;
    }

    public class HackathonDetailResponseDto
    {
        public int HackathonId { get; set; }
        public string Name { get; set; } = null!;
        public string? Season { get; set; }
        public string? Description { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string Status { get; set; } = null!;

        public List<HackathonPhaseDtos> Phases { get; set; } = new();
        public List<PrizeDto> Prizes { get; set; } = new();
    }

    public class HackathonPhaseDtos
    {
        public int PhaseId { get; set; }
        public string PhaseName { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class PrizeDto
    {
        public int PrizeId { get; set; }
        public string? PrizeName { get; set; }
        public string? PrizeType { get; set; }
        public int? Rank { get; set; }
        public string? Reward { get; set; }
    }

}
