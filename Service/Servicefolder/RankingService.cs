using AutoMapper;
using Common.DTOs.RanksDTo;
using Repositories.UnitOfWork;
using Service.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Servicefolder
{
    public class RankingService : IRankingService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;

        public RankingService(IUOW uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<List<RankingDto>> GetRankingsByHackathonAsync(int hackathonId)
        {
            var rankings = await _uow.Rankings.GetAllIncludingAsync(
                r => r.HackathonId == hackathonId,
                r => r.Team,
                r => r.Hackathon
            );

            var result = _mapper.Map<List<RankingDto>>(
                rankings.OrderByDescending(r => r.TotalScore)
            );

            return result;
        }
    }
}
