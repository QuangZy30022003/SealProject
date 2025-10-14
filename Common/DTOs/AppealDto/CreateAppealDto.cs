using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.AppealDto
{
    public class CreateAppealDto
    {
        public int AdjustmentId { get; set; }
        public int TeamId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
