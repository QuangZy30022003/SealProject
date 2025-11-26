using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.ChatDto
{
    public class ChatMessageDto
    {
        public int MessageId { get; set; }
        public int ChatGroupId { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
        public List<MessageReadStatusDto> ReadStatuses { get; set; } = new();
    }
}
