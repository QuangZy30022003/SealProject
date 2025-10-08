using Common.DTOs.TeamChallengeDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface ITeamChallengeService
    {
        Task<IEnumerable<TeamChallengeResponseDto>> RegisterTeamAsync(TeamChallengeRegisterDto dto, int currentUserId);
    }
}
