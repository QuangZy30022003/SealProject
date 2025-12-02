using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.StudentVerification
{
    public class StudentVerificationDto
    {
        public string UniversityName { get; set; }
        public string StudentCode { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string Major { get; set; } = string.Empty;

        public int YearOfAdmission { get; set; }

        public IFormFile? FrontCardImage { get; set; }

        public IFormFile? BackCardImage { get; set; }
    }
    public class StudentVerificationAdminDto
    {
        public int UserId { get; set; }
        public string UniversityName { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Major { get; set; } = string.Empty;
        public int YearOfAdmission { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? FrontCardImage { get; set; }
        public string? BackCardImage { get; set; }
    }

}
