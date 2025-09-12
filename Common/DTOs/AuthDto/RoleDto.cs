using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Repositories.Dto
{
    public class RoleDto
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }
    
    }
}
