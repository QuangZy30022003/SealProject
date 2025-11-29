using AutoMapper;
using Common.DTOs.PrizeAllocationsDto;
using Repositories.Models;
using Repositories.UnitOfWork;
using Service.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Servicefolder
{
    public class PrizeAllocationService : IPrizeAllocationService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;

        public PrizeAllocationService(IUOW uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<List<PrizeAllocationResultDto>> AutoAllocatePrizesAsyncs(int hackathonId)
        {
            // Lấy danh sách prize theo hackathon và theo rank tăng dần
            var prizes = await _uow.Prizes.GetAllAsync(
                x => x.HackathonId == hackathonId,
                q => q.OrderBy(x => x.Rank)
            );

            // Lấy danh sách rankings theo hackathon, sắp xếp điểm từ cao xuống thấp
            var rankings = (await _uow.Rankings.GetAllIncludingAsync(
                x => x.HackathonId == hackathonId,
                r => r.Team
            )).OrderByDescending(x => x.TotalScore).ToList();

            var result = new List<PrizeAllocationResultDto>();
            var allocations = new List<PrizeAllocation>();

            int count = Math.Min(prizes.Count(), rankings.Count());

            for (int i = 0; i < count; i++)
            {
                var prize = prizes.ElementAt(i);
                var ranking = rankings.ElementAt(i);

                // Lấy TeamLeader/Leader
                var leader = (await _uow.TeamMembers.GetAllIncludingAsync(
                    x => x.TeamId == ranking.TeamId &&
                         (x.RoleInTeam == "Leader" || x.RoleInTeam == "TeamLeader"),
                    tm => tm.User
                )).FirstOrDefault();

                var allocation = new PrizeAllocation
                {
                    PrizeId = prize.PrizeId,
                    TeamId = ranking.TeamId,
                    UserId = leader?.UserId,
                    AwardedAt = DateTime.UtcNow
                };

                allocations.Add(allocation);

                // Thêm vào kết quả trả về
                result.Add(new PrizeAllocationResultDto
                {
                    PrizeName = prize.PrizeName,
                    Reward = prize.Reward,
                    TeamName = ranking.Team.TeamName,
                    LeaderName = leader?.User?.FullName ?? "No Leader",
                    Rank = ranking.Rank
                });
            }

            // Xóa allocations cũ
            var oldAlloc = await _uow.PrizeAllocations.GetAllAsync(
                a => prizes.Select(p => p.PrizeId).Contains(a.PrizeId)
            );

            foreach (var old in oldAlloc)
                _uow.PrizeAllocations.Remove(old);

            await _uow.PrizeAllocations.AddRangeAsync(allocations);
            await _uow.SaveAsync();

            return result;
        }

        public async Task<List<PrizeAllocationResultDto>> GetPrizeAllocationsByHackathonAsync(int hackathonId)
        {
            // Lấy tất cả allocations cùng hackathon
            var allocations = (await _uow.PrizeAllocations.GetAllIncludingAsync(
                a => a.Prize.HackathonId == hackathonId,
                a => a.Prize,
                a => a.Team
            )).ToList();

            // Lấy tất cả TeamMembers có role Leader/TeamLeader
            var teamIds = allocations.Where(a => a.TeamId.HasValue).Select(a => a.TeamId.Value).ToList();
            var leaders = (await _uow.TeamMembers.GetAllIncludingAsync(
                tm => teamIds.Contains(tm.TeamId) &&
                      (tm.RoleInTeam == "Leader" || tm.RoleInTeam == "TeamLeader"),
                tm => tm.User
            )).ToList();

            // Lấy tất cả rankings để map rank
            var rankings = (await _uow.Rankings.GetAllAsync(
                r => r.HackathonId == hackathonId
            )).ToDictionary(r => r.TeamId, r => r.Rank);

            // Dùng AutoMapper để map các property cơ bản
            var result = allocations.Select(a =>
            {
                var dto = _mapper.Map<PrizeAllocationResultDto>(a);

                // Map thủ công LeaderName và Rank
                var leader = leaders.FirstOrDefault(l => l.TeamId == a.TeamId);
                dto.LeaderName = leader?.User?.FullName ?? "No Leader";
                dto.Rank = a.TeamId.HasValue && rankings.ContainsKey(a.TeamId.Value)
                    ? rankings[a.TeamId.Value]
                    : null;
                return dto;
            }).ToList();

            return result;
        }


    }

}
