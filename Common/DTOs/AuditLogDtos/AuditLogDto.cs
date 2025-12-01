using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.AuditLogDtos
{
    public class AuditLogDto
    {
        public int LogId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Action { get; set; }
        public Object Details { get; set; }
        public DateTime CreatedAt { get; set; }
    }

}
