using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.GroupDto
{
    public class CreateGroupsRequestDto
    {
        public int PhaseId { get; set; }   // NEW
        public int TeamsPerGroup { get; set; }
    }

    public class GroupDto
    {
        public int GroupId { get; set; }         // ID của group
        public string GroupName { get; set; }    // Tên group (A, B, C...)
        public int TrackId { get; set; }         // Track mà group thuộc về
        public List<int> TeamIds { get; set; } = new List<int>(); // Danh sách TeamId trong group
        public DateTime CreatedAt { get; set; }  // Thời gian tạo group (nếu muốn trả về)
    }

    public class GroupTeamDto
    {
        public int GroupTeamId { get; set; }
        public int GroupId { get; set; }
        public int TeamId { get; set; }
        public decimal? AverageScore { get; set; }
        public int? Rank { get; set; }
        public DateTime? JoinedAt { get; set; }

        public string TeamName { get; set; }   // Lấy từ bảng Team (nếu muốn)
    }
}
