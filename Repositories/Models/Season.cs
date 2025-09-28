using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Models
{
    public partial class Season
    {
        public int SeasonId { get; set; }
        public string Code { get; set; } = null!; // ví dụ: "SUM25", "SP25", "FALL25"
        public string Name { get; set; } = null!; // Tên hiển thị: "Summer 2025"
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public virtual ICollection<Challenge> Challenges { get; set; } = new List<Challenge>();
         public virtual ICollection<Hackathon> Hackathons { get; set; } = new List<Hackathon>();
    }
}
