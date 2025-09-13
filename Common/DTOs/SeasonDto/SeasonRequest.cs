using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.SeasonDto
{
    public class SeasonRequest
    {
        public string SeasonCode { get; set; } = null!;  // vd: "SUM25"
        public string Name { get; set; } = null!;        // vd: "Summer 2025"
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class SeasonResponse
    {
        public int SeasonId { get; set; }
        public string SeasonCode { get; set; } = null!;
        public string Name { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class SeasonUpdateDto
    {
        public string SeasonCode { get; set; } = null!;
        public string Name { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
