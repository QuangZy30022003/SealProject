using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.ChallengeDto
{
    public class ChallengeCreateUnifiedDto
    {
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public int HackathonId { get; set; }

        // Nếu upload từ link thì truyền link vào
        public IFormFile? File { get; set; }
    }




    public class ChallengeDto
    {
        public int ChallengeId { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string? FilePath { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public int HackathonId { get; set; }
        public int UserId { get; set; }
    }

    public class ChallengeStatusDto
    {
        public string Status { get; set; } = null!; // Approved | Rejected
    }

    public class ChallengePartnerUpdateDto
    {

        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string? FilePath { get; set; }
        public int HackathonId { get; set; }
        public IFormFile? File { get; set; }
    }


}
