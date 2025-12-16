using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.QualifiedFinealTeamDto
{
    public class QualifiedTeamDto
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; }
        public decimal AverageScore { get; set; }
        public int GroupId { get; set; }
    }

    public class QualifiedTeamDtos
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; }
        public int GroupId { get; set; }
        public string GroupName { get; set; }

        public string TrackName { get; set; }
    }

    public class TeamScoreAdjustedDto
    {
        public int TeamId { get; set; }
        public int GroupId { get; set; }
        public int TrackId { get; set; }

        public decimal OriginalScore { get; set; }
        public decimal PenaltyPoints { get; set; }
        public decimal AdjustedScore { get; set; }
    }

}
