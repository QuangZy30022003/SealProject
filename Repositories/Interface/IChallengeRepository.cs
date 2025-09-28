using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Interface
{
    public interface IChallengeRepository : IRepository<Challenge>
    {
        Task<IEnumerable<Challenge>> GetChallengesWithSeasonAndUserAsync();

       //Task<List<Challenge>> GetApprovedChallengesByHackathonAsync(int hackathonId);
    }
}
