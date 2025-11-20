using Common.DTOs.ScoreDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.Submission
{
    public class SubmissionCreateDto
    {
        public int TeamId { get; set; }
        public int PhaseId { get; set; }
        public string Title { get; set; }
        public string FilePath { get; set; }
    }

    public class SubmissionUpdateDto
    {
        public string Title { get; set; }
        public string FilePath { get; set; }
    }

    public class SubmissionFinalDto
    {
        public int SubmissionId { get; set; }
        public int TeamId { get; set; }
    }

    public class SubmissionResponseDto
    {
        public int SubmissionId { get; set; }
        public string TeamName { get; set; }
        public string PhaseName { get; set; }
        public string Title { get; set; }
        public string FilePath { get; set; }
        public DateTime SubmittedAt { get; set; }
        public int SubmittedBy { get; set; }
        public bool IsFinal { get; set; }

        public string TrackName { get; set; }
    }

}
