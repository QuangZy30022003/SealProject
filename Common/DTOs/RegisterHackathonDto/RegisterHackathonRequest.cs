    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    namespace Common.DTOs.RegisterHackathonDto
    {
        public class RegisterHackathonRequest
        {
            public int HackathonId { get; set; }
            public string Link { get; set; } = string.Empty;
        }

        public class CancelHackathonRegistrationRequest
        {
            public int HackathonId { get; set; }
            public string CancelReason { get; set; } = string.Empty;
        }
        public class RestoreHackathonRegistrationRequest
        {
            public int HackathonId { get; set; }
        }

        public class ApproveTeamRequest
        {
            public int HackathonId { get; set; }
            public int TeamId { get; set; }
        }
        public class RejectTeamRequest
        {
            public int HackathonId { get; set; }
            public int TeamId { get; set; }
            public string CancelReason { get; set; } = string.Empty;
        }

        public class HackathonRegistrationDto
        {
            public int RegistrationId { get; set; }
            public int HackathonId { get; set; }
            public string HackathonName { get; set; } = string.Empty;
            public int TeamId { get; set; }
            public string TeamName { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string Link { get; set; } = string.Empty;
            public DateTime RegisteredAt { get; set; }
            public DateTime? CancelledAt { get; set; }
            public string? CancelReason { get; set; }
        }


    }
