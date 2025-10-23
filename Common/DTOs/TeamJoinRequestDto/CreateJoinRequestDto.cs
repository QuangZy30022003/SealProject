using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.TeamJoinRequestDto
{
    public class CreateJoinRequestDto
    {
        public int TeamId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
