using Common.DTOs.PrizeAllocationsDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface IPrizeAllocationService
    {
        Task<List<PrizeAllocationResultDto>> AutoAllocatePrizesAsyncs(int hackathonId);
        Task<List<PrizeAllocationResultDto>> GetPrizeAllocationsByHackathonAsync(int hackathonId);

    }

}
