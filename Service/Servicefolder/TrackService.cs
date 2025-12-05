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
            // Lấy phase từ PhaseId
            var phase = await _uow.HackathonPhases.GetByIdAsync(dto.PhaseId);
            if (phase == null)
                throw new Exception("Phase not found");

            var hackathonId = phase.HackathonId;

            // Lấy tất cả phases trong hackathon
            var phases = await _uow.HackathonPhases.GetAllAsync(
                p => p.HackathonId == hackathonId
            );

            if (!phases.Any())
                throw new Exception("Hackathon does not contain any phases");

            // Lấy phase đầu tiên theo StartDate
            var firstPhase = phases
                .Where(p => p.StartDate != null)
                .OrderBy(p => p.StartDate)
                .FirstOrDefault();

            // Nếu StartDate rỗng → dùng EndDate
            if (firstPhase == null)
            {
                firstPhase = phases
                    .Where(p => p.EndDate != null)
                    .OrderBy(p => p.EndDate)
                    .FirstOrDefault();
            }

            // Nếu cả StartDate và EndDate rỗng → fallback theo PhaseId
            if (firstPhase == null)
            {
                firstPhase = phases.OrderBy(p => p.PhaseId).First();
            }

            // Kiểm tra phase được gửi lên có phải phase đầu tiên không
            if (firstPhase.PhaseId != dto.PhaseId)
                throw new Exception("Tracks can only be created for the first phase of this hackathon");

            // Tạo track
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

            // Lấy phase chứa track
            var phase = await _uow.HackathonPhases.GetByIdAsync(track.PhaseId);
            if (phase == null)
                throw new Exception("Phase not found for this track");

            var now = DateTime.UtcNow; // hoặc DateTime.Now nếu bạn lưu giờ local

            // Nếu Phase có StartDate → không cho xóa khi đã đến StartDate
            if (phase.StartDate != null)
            {
                if (now >= phase.StartDate.Value)
                    throw new Exception("This track cannot be deleted because its phase has already started.");
            }

            // Nếu không có StartDate nhưng có EndDate → khóa khi đã tới EndDate
            if (phase.StartDate == null && phase.EndDate != null)
            {
                if (now >= phase.EndDate.Value)
                    throw new Exception("This track cannot be deleted because its phase has ended.");
            }

            // Nếu cả startDate và endDate đều null → cho phép xóa
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

        public async Task<List<TrackRespone>> GetTracksByPhaseIdAsync(int phaseId)
        {
            var tracks = await _uow.Tracks.GetAllIncludingAsync(
                t => t.PhaseId == phaseId,
                t => t.Challenges
            );
            return _mapper.Map<List<TrackRespone>>(tracks);
        }
    }
}
