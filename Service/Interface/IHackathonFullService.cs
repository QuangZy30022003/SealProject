using Common.DTOs.HackathonDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface IHackathonFullService
    {
        Task<int> CreateFullHackathonAsync(HackathonFullCreateDto dto, int userid);
    }
}
