using Common.DTOs.RanksDTo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface IRankingService
    {
        Task<List<RankingDto>> GetRankingsByHackathonAsync(int hackathonId);
    }
}
