﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.DTOs.TeamMemberDto;

namespace Service.Interface
{
    public interface ITeamMemberService
    {
        Task<string> KickMemberAsync(int teamId, int memberId, int currentUserId);
        Task<string> LeaveTeamAsync(int teamId, int userId);
        //Task<string> ChangeRoleAsync(int teamId, int memberId, string newRole, int currentUserId);
        Task<IEnumerable<TeamMemberDto>> GetTeamMembersAsync(int teamId);
    }
}
