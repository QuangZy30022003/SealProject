using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.MentorVerificationDto
{
    public class MentorVerificationCreateDto
    {
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Position { get; set; } = "";
        public string ReasonToBecomeMentor { get; set; } = "";
        public int HackathonId { get; set; }
        public int? ChapterId { get; set; }
    }

    public class MentorVerificationResponseDto
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Position { get; set; }
        public string CV { get; set; }
        public string ReasonToBecomeMentor { get; set; }
        public string? RejectReason { get; set; }
        public string Status { get; set; }

        public int HackathonId { get; set; }
        public int UserId { get; set; }
        public int? ChapterId { get; set; }
        public string? ChapterName { get; set; } // chỉ để trả ra API, không insert vào DB
    }


}
