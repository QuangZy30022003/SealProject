using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.ChapterDto
{
    public class ChapterDto
    {
        public int ChapterId { get; set; }
        public string ChapterName { get; set; } = null!;
        public string? Description { get; set; }
        public int? LeaderId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
