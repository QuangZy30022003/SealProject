using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs.TrackDto
{
    public class TrackRespone
    {
        public int TrackId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int PhaseId { get; set; }
        public int? ChallengeId { get; set; }
    }
    public class CreateTrackDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int PhaseId { get; set; }
    }

    public class UpdateTrackDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int PhaseId { get; set; }
    }

 

}
