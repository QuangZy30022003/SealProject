using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.NotificationDto
{
    public class CreateNotificationDto
    {
        public int UserId { get; set; }
        public string Message { get; set; }
    }
}
