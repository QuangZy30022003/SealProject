using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Interface
{
    public interface IHackathonPhaseRepository : IRepository<HackathonPhase>
    {
        Task<List<HackathonPhase>> GetByHackathonIdAsync(int hackathonId);
    }
}
