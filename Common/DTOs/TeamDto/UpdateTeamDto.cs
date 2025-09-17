using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.TeamDto
{
    public class UpdateTeamDto
    {
        public string TeamName { get; set; } = string.Empty;
        public int ChapterId { get; set; }
    }
}
