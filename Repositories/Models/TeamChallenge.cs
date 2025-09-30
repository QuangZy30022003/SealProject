﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Models
{
    public partial class TeamChallenge
    {
        public int TeamChallengeId { get; set; }   // thêm khóa chính
        public int TeamId { get; set; }
        public int HackathonId { get; set; }
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        public virtual Team Team { get; set; } = null!;
        public virtual Hackathon Hackathon { get; set; } = null!;
    }

}
