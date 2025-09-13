using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.TeamDto
{
    public class CreateTeamDto
    {
        public string TeamName { get; set; } = null!;
        public int? ChapterId { get; set; }
        public int? LeaderId { get; set; }
    }
}
