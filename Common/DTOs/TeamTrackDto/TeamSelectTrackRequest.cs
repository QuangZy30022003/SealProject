using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.TeamTrackDto
{
    public class TeamSelectTrackRequest
    {
        public int TeamId { get; set; }
        public int TrackId { get; set; }
    }

    public class TeamSelectTrackResponse
    {
        public int TeamTrackSelectionId { get; set; }
        public int TeamId { get; set; }
        public int TrackId { get; set; }
        public DateTime SelectedAt { get; set; }
    }

    public class TeamTrackByPhaseResponseDto
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; } = null!;

        public int TrackId { get; set; }
        public string TrackName { get; set; } = null!;

        public DateTime SelectedAt { get; set; }
    }
}
