using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.PartnerProfileDto
{

    public class PartnerProfileDto
    {
        public int PartnerProfileId { get; set; }
        public int UserId { get; set; }
        public string CompanyName { get; set; }
        public string LogoUrl { get; set; }
        public string Website { get; set; }
        public string Description { get; set; }
    }
    public class PartnerProfileCreateDto
    {
        public string CompanyName { get; set; }
        public IFormFile LogoFile { get; set; }   // ⬅️ Upload file
        public string Website { get; set; }
        public string Description { get; set; }
    }

    public class PartnerProfileUpdateDto
    {
        public string CompanyName { get; set; }
        public IFormFile LogoFile { get; set; }   // ⬅️ Upload file
        public string Website { get; set; }
        public string Description { get; set; }
    }

}
