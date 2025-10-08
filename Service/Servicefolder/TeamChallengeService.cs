using AutoMapper;
using Common.DTOs.TeamChallengeDto;
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
    public class TeamChallengeService : ITeamChallengeService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;

        public TeamChallengeService(IUOW uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<IEnumerable<TeamChallengeResponseDto>> RegisterTeamAsync(TeamChallengeRegisterDto dto, int currentUserId)
        {
            // 1️⃣ Kiểm tra team
            var team = await _uow.Teams.GetByIdAsync(dto.TeamId);
            if (team == null)
                throw new Exception("Team not found!");

            // 2️⃣ Chỉ TeamLeader mới được đăng ký
            if (team.LeaderId != currentUserId)
                throw new Exception("Only the Team Leader can register the team!");

            // 3️⃣ Kiểm tra hackathon
            var hackathon = await _uow.Hackathons.GetByIdAsync(dto.HackathonId);
            if (hackathon == null)
                throw new Exception("Hackathon not found!");

            // 4️⃣ Team phải có mentor
            var hasMentor = (await _uow.MentorAssignments.GetAllAsync(m => m.TeamId == dto.TeamId)).Any();
            if (!hasMentor)
                throw new Exception("Team must be assigned to a mentor before registering!");

            // 5️⃣ Kiểm tra team đã đăng ký hackathon này chưa
            var exist = await _uow.TeamChallenges.GetAllAsync(tc =>
                tc.TeamId == dto.TeamId && tc.HackathonId == dto.HackathonId);
            if (exist.Any())
                throw new Exception("This team already registered for this hackathon!");

            // 6️⃣ Lấy tất cả phase thuộc hackathon
            var phases = await _uow.HackathonPhases.GetAllAsync(p => p.HackathonId == dto.HackathonId);
            if (!phases.Any())
                throw new Exception("Hackathon must have at least one phase before registration!");

            // 7️⃣ Tạo TeamChallenge cho từng phase
            var now = DateTime.UtcNow;
            var newChallenges = new List<TeamChallenge>();
            foreach (var phase in phases)
            {
                var teamChallenge = new TeamChallenge
                {
                    TeamId = dto.TeamId,
                    HackathonId = dto.HackathonId,
                    PhaseId = phase.PhaseId,    
                    RegisteredAt = now,
                    Status = true
                };
                newChallenges.Add(teamChallenge);
                await _uow.TeamChallenges.AddAsync(teamChallenge);
            }

            await _uow.SaveAsync();

            // 8️⃣ Lấy dữ liệu có include để map sang DTO
            var result = await _uow.TeamChallenges.GetAllIncludingAsync(
                tc => tc.TeamId == dto.TeamId && tc.HackathonId == dto.HackathonId,
                tc => tc.Team,
                tc => tc.Hackathon
            );

            return _mapper.Map<IEnumerable<TeamChallengeResponseDto>>(result);
        }
    }
}