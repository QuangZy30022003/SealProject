using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.ChallengeDto
{
    //public class ChallengeCreateDto
    //{
    //    public string Title { get; set; } = null!;
    //    public string? Description { get; set; }
    //    public int SeasonId { get; set; }
    //}

    //public class ChallengeCreateLinkDto
    //{
    //    public string Title { get; set; } = null!;
    //    public string? Description { get; set; }
    //    public int SeasonId { get; set; }
    //    public string FilePath { get; set; } = null!;
    //}

    public class ChallengeCreateUnifiedDto
    {
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public int SeasonId { get; set; }

        // Nếu upload từ link thì truyền link vào
        public string? FilePath { get; set; }
    }


    public class ChallengeUpdateDto
    {
        public int ChallengeId { get; set; }   // để biết cái nào update
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string? FilePath { get; set; }
        public int SeasonId { get; set; }
        public IFormFile? File { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
    }


    public class ChallengeDto
    {
        public int ChallengeId { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string? FilePath { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public int SeasonId { get; set; }
        public string SeasonName { get; set; } = null!;
        public int UserId { get; set; }
        public string UserName { get; set; } = null!;
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
        public int SeasonId { get; set; }
        public IFormFile? File { get; set; }
    }

}
