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
        public int? LeaderId { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
