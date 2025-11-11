using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.CriterionDTO
{
    // CriterionCreateDto.cs
    public class CriterionCreateDto
    {
        public int PhaseId { get; set; }
        public int? TrackId { get; set; }

        public List<CriterionItemDto> Criteria { get; set; } = new List<CriterionItemDto>();
    }

    public class CriterionItemDto
    {
        public string Name { get; set; }
        public decimal Weight { get; set; }
    }

    // CriterionUpdateDto.cs
    public class CriterionUpdateDto
    {
        public int? TrackId { get; set; }
        public string Name { get; set; }
        public decimal Weight { get; set; }
    }

    // CriterionResponseDto.cs
    public class CriterionResponseDto
    {
        public int CriteriaId { get; set; }
        public int PhaseId { get; set; }
        public int? TrackId { get; set; }
        public string Name { get; set; }
        public decimal Weight { get; set; }
    }

}
