using AutoMapper;
using Common.DTOs.TeamMemberDto;
using Repositories.UnitOfWork;
using Service.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Servicefolder
{
    public class TeamMemberService : ITeamMemberService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;

        public TeamMemberService(IUOW uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }


        public async Task<string> KickMemberAsync(int teamId, int memberId, int currentUserId)
        {
            var team = await _uow.Teams.GetByIdAsync(teamId);
            if (team == null) throw new Exception("Team not found.");

            if (team.TeamLeaderId != currentUserId)
                throw new UnauthorizedAccessException("Only leader can kick members.");

            var member = await _uow.TeamMembers.FirstOrDefaultAsync(m => m.TeamId == teamId && m.UserId == memberId);
            if (member == null) throw new Exception("Member not found in team.");

            if (member.UserId == team.TeamLeaderId)
                throw new Exception("Leader cannot be kicked.");

            _uow.TeamMembers.Remove(member);
            await _uow.SaveAsync();

            return "Member has been kicked successfully.";
        }

        public async Task<string> LeaveTeamAsync(int teamId, int userId)
        {
            var member = await _uow.TeamMembers.FirstOrDefaultAsync(m => m.TeamId == teamId && m.UserId == userId);
            if (member == null) throw new Exception("You are not in this team.");

            var team = await _uow.Teams.GetByIdAsync(teamId);
            if (team.TeamLeaderId == userId)
                throw new Exception("Leader cannot leave the team. Please transfer leadership first.");

            _uow.TeamMembers.Remove(member);
            await _uow.SaveAsync();

            return "You have left the team.";
        }

        public async Task<string> ChangeLeaderAsync(int teamId, int newLeaderId, int currentLeaderId)
        {
            var team = await _uow.Teams.GetByIdAsync(teamId);
            if (team == null)
                throw new Exception("Team not found.");

            if (team.TeamLeaderId != currentLeaderId)
                throw new UnauthorizedAccessException("Only the current leader can transfer leadership.");

            // 🚫 Không cho chuyển cho chính mình
            if (newLeaderId == currentLeaderId)
                throw new Exception("Cannot transfer leadership to yourself.");

            var newLeader = await _uow.TeamMembers.FirstOrDefaultAsync(m => m.TeamId == teamId && m.UserId == newLeaderId);
            if (newLeader == null)
                throw new Exception("The specified user is not a member of this team.");

            // Cập nhật role trong TeamMembers
            var oldLeader = await _uow.TeamMembers.FirstOrDefaultAsync(m => m.TeamId == teamId && m.UserId == currentLeaderId);
            if (oldLeader != null)
            {
                oldLeader.RoleInTeam = "Member";
                _uow.TeamMembers.Update(oldLeader);
            }

            newLeader.RoleInTeam = "Leader";
            _uow.TeamMembers.Update(newLeader);

            // Cập nhật Team.TeamLeaderId
            team.TeamLeaderId = newLeaderId;
            _uow.Teams.Update(team);

            await _uow.SaveAsync();
            return $"Leadership has been successfully transferred to user ID {newLeaderId}.";
        }

        public async Task<IEnumerable<TeamMemberDto>> GetTeamMembersAsync(int teamId)
        {
            var members = await _uow.TeamMembers.GetAllIncludingAsync(
                m => m.TeamId == teamId,
                m => m.User
            );

            return _mapper.Map<IEnumerable<TeamMemberDto>>(members);
        }

        public async Task<bool> CheckLeaderAsync(int teamId, int userId)
        {
            var member = await _uow.TeamMembers.FirstOrDefaultAsync(
                m => m.TeamId == teamId && m.UserId == userId
            );

            if (member == null)
                throw new Exception("User is not in this team.");

            // Điều kiện leader
            return member.RoleInTeam.Equals("Leader", StringComparison.OrdinalIgnoreCase)
                || member.RoleInTeam.Equals("TeamLeader", StringComparison.OrdinalIgnoreCase);
        }

    }
}
