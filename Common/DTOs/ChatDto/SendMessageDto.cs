using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.ChatDto
{
    public class SendMessageDto
    {
        public int ChatGroupId { get; set; }  // ✅ Chỉ cần ChatGroupId
        public string Content { get; set; } = string.Empty;
    }
}
