using AutoMapper;
using Common.DTOs.ChallengeDto;
using Common.DTOs.TrackDto;
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
    public class TrackService : ITrackService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;

        public TrackService(IUOW uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        // ✅ Lấy tất cả track theo phase
        public async Task<List<TrackRespone>> GetTracksdAsync()
        {
            var tracks = await _uow.Tracks.GetAllIncludingAsync(
                null,
                t => t.Challenges
            );
            return _mapper.Map<List<TrackRespone>>(tracks);
        }

        public async Task<TrackRespone?> GetTrackByIdAsync(int id)
        {
            var track = await _uow.Tracks.GetAllIncludingAsync(
                t => t.TrackId == id,
                t => t.Challenges
            );
            var result = track.FirstOrDefault();
            return result == null ? null : _mapper.Map<TrackRespone>(result);
        }
        // ✅ Tạo track (ChallengeId luôn = null)
        public async Task<TrackRespone> CreateTrackAsync(CreateTrackDto dto)
        {
            var track = new Track
            {
                Name = dto.Name,
                Description = dto.Description,
                PhaseId = dto.PhaseId
            };

            await _uow.Tracks.AddAsync(track);
            await _uow.SaveAsync();

            return _mapper.Map<TrackRespone>(track);
        }

        // ✅ Cập nhật track
        public async Task<TrackRespone?> UpdateTrackAsync(int id, UpdateTrackDto dto)
        {
            var track = await _uow.Tracks.GetByIdAsync(id);
            if (track == null)
                return null;

            track.Name = dto.Name;
            track.Description = dto.Description;
            track.PhaseId = dto.PhaseId;

            _uow.Tracks.Update(track);
            await _uow.SaveAsync();

            return _mapper.Map<TrackRespone>(track);
        }

        // ✅ Xóa track
        public async Task<bool> DeleteTrackAsync(int id)
        {
            var track = await _uow.Tracks.GetByIdAsync(id);
            if (track == null)
                return false;

            _uow.Tracks.Remove(track);
            await _uow.SaveAsync();
            return true;
        }

        public async Task<RandomChallengeTrackResponse?> AssignRandomChallengesToTrackAsync(RandomChallengeTrackRequest request)
        {
            // Lấy track
            var track = await _uow.Tracks.GetByIdAsync(request.TrackId);
            if (track == null) return null;

            var phaseId = track.PhaseId;


            // Lấy challenge đã được gán trong phase này
            var usedChallengeIds = (await _uow.Challenges.GetAllIncludingAsync(
                c => c.TrackId != null && c.Track.PhaseId == phaseId,
                c => c.Track
                )).Select(c => c.ChallengeId).ToList();

            // Lọc challenge hợp lệ
            var challenges = await _uow.Challenges.GetAllIncludingAsync(
                c => request.ChallengeIds.Contains(c.ChallengeId)
                    && c.Status == "Complete"
                    && !usedChallengeIds.Contains(c.ChallengeId),
                c => c.User
            );

            // không còn challenge hợp lệ
            if (challenges == null || !challenges.Any())
                return null;

            var rnd = new Random();
            var selected = challenges
                .OrderBy(x => rnd.Next())
                .Take(request.Quantity)
                .ToList();

            // Gán tất cả selected challenge vào Track
            foreach (var c in selected)
            {
                c.TrackId = track.TrackId;
                _uow.Challenges.Update(c);
            }

            await _uow.SaveAsync();

            return new RandomChallengeTrackResponse
            {
                TrackId = track.TrackId,
                SelectedChallenges = selected.Select(c => new ChallengeDto
                {
                    ChallengeId = c.ChallengeId,
                    Title = c.Title,
                    Description = c.Description,
                    FilePath = c.FilePath,
                    Status = c.Status,
                    CreatedAt = c.CreatedAt,
                    HackathonId = c.HackathonId,
                    UserId = c.UserId ?? 0,
                    UserName = c.User?.FullName ?? ""
                }).ToList()
            };
        }
    }
}
