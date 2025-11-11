using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.TeamDto
{
    public class TeamDto
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; } = null!;
        public int? ChapterId { get; set; }
        public string ChapterName { get; set; } = null!;
        public int? TeamLeaderId { get; set; }
        public string TeamLeaderName { get; set; } = null!;
        public int? HackathonId { get; set; }
        public string HackathonName { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
