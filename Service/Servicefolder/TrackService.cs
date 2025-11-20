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
            var tracks = await _uow.Tracks.GetAllAsync();
            return _mapper.Map<List<TrackRespone>>(tracks);
        }

        public async Task<TrackRespone?> GetTrackByIdAsync(int id)
        {
            var track = await _uow.Tracks.GetByIdAsync(id);
            return track == null ? null : _mapper.Map<TrackRespone>(track);
        }
        // ✅ Tạo track (ChallengeId luôn = null)
        public async Task<TrackRespone> CreateTrackAsync(CreateTrackDto dto)
        {
            var track = new Track
            {
                Name = dto.Name,
                Description = dto.Description,
                PhaseId = dto.PhaseId,
                ChallengeId = null
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

            // Lấy các challenge đã dùng trong phase
            var usedChallengeIds = (await _uow.Tracks.GetAllAsync(
                t => t.PhaseId == phaseId && t.ChallengeId != null))
                .Select(t => t.ChallengeId.Value)
                .ToList();

            // Lọc challenge hợp lệ
            var challenges = await _uow.Challenges.GetAllIncludingAsync(
                c => request.ChallengeIds.Contains(c.ChallengeId)
                    && c.Status == "Complete"
                    && !usedChallengeIds.Contains(c.ChallengeId),
                c => c.User
            );

            if (!challenges.Any())
                return null;

            var rnd = new Random();
            var selected = challenges
                .OrderBy(x => rnd.Next())
                .Take(request.Quantity)
                .ToList();

            // Gán challenge đầu tiên cho Track
            track.ChallengeId = selected.First().ChallengeId;
            _uow.Tracks.Update(track);
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
