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
            if (team == null) throw new Exception("Team not found");

            if (team.LeaderId != currentUserId)
                throw new UnauthorizedAccessException("Only leader can kick members");

            var member = await _uow.TeamMembers.FirstOrDefaultAsync(m => m.TeamId == teamId && m.UserId == memberId);
            if (member == null) throw new Exception("Member not found in team");

            if (member.UserId == team.LeaderId)
                throw new Exception("Leader cannot be kicked");

            _uow.TeamMembers.Remove(member);
            await _uow.SaveAsync();

            return "Member has been kicked successfully.";
        }

        public async Task<string> LeaveTeamAsync(int teamId, int userId)
        {
            var member = await _uow.TeamMembers.FirstOrDefaultAsync(m => m.TeamId == teamId && m.UserId == userId);
            if (member == null) throw new Exception("You are not in this team");

            var team = await _uow.Teams.GetByIdAsync(teamId);
            if (team.LeaderId == userId)
                throw new Exception("Leader cannot leave the team. Please transfer leadership first.");

            _uow.TeamMembers.Remove(member);
            await _uow.SaveAsync();

            return "You have left the team.";
        }

        //public async Task<string> ChangeRoleAsync(int teamId, int memberId, string newRole, int currentUserId)
        //{
        //    var team = await _uow.Teams.GetByIdAsync(teamId);
        //    if (team == null) throw new Exception("Team not found");

        //    if (team.LeaderId != currentUserId)
        //        throw new UnauthorizedAccessException("Only leader can change roles");

        //    var member = await _uow.TeamMembers.FirstOrDefaultAsync(m => m.TeamId == teamId && m.UserId == memberId);
        //    if (member == null) throw new Exception("Member not found");

        //    member.RoleInTeam = newRole;
        //    _uow.TeamMembers.Update(member);
        //    await _uow.SaveAsync();

        //    return $"Role of user {memberId} changed to {newRole}";
        //}

        public async Task<IEnumerable<TeamMemberDto>> GetTeamMembersAsync(int teamId)
        {
            var members = await _uow.TeamMembers.GetAllIncludingAsync(
                m => m.TeamId == teamId,
                m => m.User
            );


            return _mapper.Map<IEnumerable<TeamMemberDto>>(members);
        }
    }
}
