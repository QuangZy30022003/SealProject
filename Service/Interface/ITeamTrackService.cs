using Common.DTOs.TeamTrackDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface ITeamTrackService
    {
        Task<TeamSelectTrackResponse?> SelectTrackAsync(int userIdFromToken, TeamSelectTrackRequest request);
    }
}
