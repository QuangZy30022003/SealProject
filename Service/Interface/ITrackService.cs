using Common.DTOs.ChallengeDto;
using Common.DTOs.TrackDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface ITrackService
    {
        Task<bool> DeleteTrackAsync(int id);
        Task<TrackRespone?> UpdateTrackAsync(int id, UpdateTrackDto dto);
        Task<TrackRespone> CreateTrackAsync(CreateTrackDto dto);
        Task<List<TrackRespone>> GetTracksdAsync();

        Task<TrackRespone?> GetTrackByIdAsync(int id);
        Task<RandomChallengeTrackResponse?> AssignRandomChallengesToTrackAsync(RandomChallengeTrackRequest request);
    }
}
