using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.TeamMemberDto
{
    public class TeamMemberDto
    {
        public int UserId { get; set; }
        public int TeamId { get; set; }
        public string RoleInTeam { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty; // hiển thị thêm tên user
        public string Email { get; set; } = string.Empty;    // hiển thị thêm email
    }
}
