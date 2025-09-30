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

        public async Task<TeamChallengeResponseDto> RegisterTeamAsync(TeamChallengeRegisterDto dto, int currentUserId)
        {
            // 1. Validate Team
            var team = await _uow.Teams.GetByIdAsync(dto.TeamId);
            if (team == null) throw new Exception("Team not found!");

            // 2. Chỉ TeamLeader mới có quyền đăng ký
            if (team.LeaderId != currentUserId)
                throw new Exception("Only the Team Leader can register the team!");

            // 3. Validate Hackathon
            var hackathon = await _uow.Hackathons.GetByIdAsync(dto.HackathonId);
            if (hackathon == null) throw new Exception("Hackathon not found!");

            // 4. Kiểm tra xem Team đã được assign Mentor chưa
            var hasMentor = (await _uow.MentorAssignments.GetAllAsync(
                filter: m => m.TeamId == dto.TeamId
            )).Any();

            if (!hasMentor)
                throw new Exception("Team must be assigned to a mentor before registering!");

            // 5. Kiểm tra xem team đã đăng ký hackathon này chưa
            var exist = await _uow.TeamChallenges.GetAllAsync(
                filter: tc => tc.TeamId == dto.TeamId && tc.HackathonId == dto.HackathonId
            );
            if (exist.Any())
                throw new Exception("This team already registered for this hackathon!");

            // 6. Map DTO -> Entity
            var entity = _mapper.Map<TeamChallenge>(dto);
            await _uow.TeamChallenges.AddAsync(entity);
            await _uow.SaveAsync();

            // 7. Load navigation để mapper có dữ liệu
            var loaded = await _uow.TeamChallenges.GetAllAsync(
                filter: x => x.TeamChallengeId == entity.TeamChallengeId,
                includeProperties: "Team,Hackathon"
            );

            return _mapper.Map<TeamChallengeResponseDto>(loaded.First());
        }
    }
}
