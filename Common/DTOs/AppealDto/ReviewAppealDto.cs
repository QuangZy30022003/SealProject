using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.AppealDto
{
    public class ReviewAppealDto
    {
        public string Status { get; set; } = string.Empty; // Approved | Rejected
        public string AdminResponse { get; set; } = string.Empty;
    }
}
