using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface IPhaseChallengeService
    {
        Task<bool> AssignRandomChallengesToHackathonPhasesAsync(int hackathonId, int perPhase);
    }
}
