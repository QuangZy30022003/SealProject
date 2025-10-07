using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Models
{
    public partial class StudentVerification
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int VerificationId { get; set; }
        public int UserId { get; set; }
        public string UniversityName { get; set; } = null!;
        public string StudentEmail { get; set; } = null!;
        public string StudentCode { get; set; } = null!;
        public int YearOfAdmission { get; set; }
        public string FullName { get; set; } = null!;
        public string? Major { get; set; }
        public string? FrontCardImage { get; set; }
        public string? BackCardImage { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public virtual User User { get; set; } = null!;
    }
}
