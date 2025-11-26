using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.ChatDto
{
    public class ChatGroupDto
    {
        public int ChatGroupId { get; set; }
        public int MentorId { get; set; }
        public string MentorName { get; set; } = string.Empty;
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public int HackathonId { get; set; }
        public string HackathonName { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public string? LastMessageContent { get; set; } // Tin nhắn cuối cùng
        public int UnreadCount { get; set; } // Số tin nhắn chưa đọc (tính theo userId)
    }
}
