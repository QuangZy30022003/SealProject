using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.ScoreDto
{
    public class ScoreHistoryDto
    {
        public int HistoryId { get; set; }
        public int ScoreId { get; set; }
        public int SubmissionId { get; set; }
        public string SubmissionTitle { get; set; }

        public int JudgeId { get; set; }

        public int CriteriaId { get; set; }
        public string CriteriaName { get; set; }

        public int OldScore { get; set; }
        public string OldComment { get; set; }

        public DateTime ChangedAt { get; set; }
        public string ChangeReason { get; set; }

        public int? ChangedBy { get; set; }
        public string ChangedByName { get; set; }
    }

}
