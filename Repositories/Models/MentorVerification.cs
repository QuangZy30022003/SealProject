using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Models
{
    public class MentorVerification
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Position { get; set; } = "";
        public string CV { get; set; } = "";
        public string ReasonToBecomeMentor { get; set; } = "";
        public string? RejectReason { get; set; }
        public string Status { get; set; } = "Pending";

        public int HackathonId { get; set; }
        public int UserId { get; set; }
        public int? ChapterId { get; set; }  // <-- chỉ lưu Id

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public User User { get; set; }
        public Hackathon Hackathon { get; set; }
        public Chapter Chapter { get; set; } // EF will join ChapterName if needed
    }


}
