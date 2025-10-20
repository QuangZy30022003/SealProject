using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.CriterionDTO
{
    public class CriterionDetailDTO
    {
        public int CriterionDetailId { get; set; }
        public string Description { get; set; } = null!;
        public int MaxScore { get; set; }
    }

    public class CriterionDetailCreateDTO
    {
        public string Description { get; set; } = null!;
        public int MaxScore { get; set; }
    }

    public class CriterionCreateDTO
    {
        public int PhaseChallengeId { get; set; }
        public string Name { get; set; } = null!;
        public decimal Weight { get; set; }
        public List<CriterionDetailCreateDTO>? Details { get; set; }
    }

    public class CriterionUpdateDTO
    {
        public int CriteriaId { get; set; }
        public string Name { get; set; } = null!;
        public decimal Weight { get; set; }
        public List<CriterionDetailCreateDTO>? Details { get; set; }
    }

    public class CriterionReadDTO
    {
        public int CriteriaId { get; set; }
        public string Name { get; set; } = null!;
        public decimal Weight { get; set; }
        public int PhaseChallengeId { get; set; }
        public string? PhaseChallengeName { get; set; }
        public List<CriterionDetailDTO>? Details { get; set; }
    }
}
