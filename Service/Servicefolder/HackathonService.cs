using AutoMapper;
using Common.DTOs.HackathonDto;
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
    public class HackathonService : IHackathonService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;

        public HackathonService(IUOW uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<IEnumerable<HackathonResponseDto>> GetAllAsync()
        {
            var entities = await _uow.Hackathons.GetAllIncludingAsync(null,
                  h => h.Season);

            var ordered = entities
    .OrderByDescending(h => h.HackathonId)
    .ToList();


            return _mapper.Map<IEnumerable<HackathonResponseDto>>(ordered);
        }

        public async Task<HackathonDetailResponseDto?> GetByIdAsync(int id)
        {
            var entity = await _uow.HackathonPhaseRepository.GetHackathonDetailAsync(id);
            if (entity == null) return null;

            return _mapper.Map<HackathonDetailResponseDto>(entity);
        }



        public async Task<HackathonResponseDto> CreateHackathonAsync(HackathonCreateDto dto, int userId)
        {
            var season = await _uow.Seasons.GetByIdAsync(dto.SeasonId);
            if (season == null)
                throw new ArgumentException("Season not found");

            var seasonStart = DateOnly.FromDateTime(season.StartDate);
            var seasonEnd = DateOnly.FromDateTime(season.EndDate);

            // Rule 1: EndDate phải sau StartDate
            if (dto.EndDate <= dto.StartDate)
                throw new ArgumentException("EndDate must be later than StartDate");

            // Rule 2: Ngày phải nằm trong Season
            if (dto.StartDate < seasonStart || dto.EndDate > seasonEnd)
                throw new ArgumentException(
                    $"Hackathon dates must fall within the season ({seasonStart:yyyy-MM-dd} - {seasonEnd:yyyy-MM-dd})"
                );

            var hackathon = new Hackathon
            {
                Name = dto.Name,
                SeasonId = dto.SeasonId,
                Description = dto.Description,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                CreatedBy = userId,
                  Status = dto.Status ?? "Pending"
            };

            await _uow.Hackathons.AddAsync(hackathon);
            await _uow.SaveAsync();

            return _mapper.Map<HackathonResponseDto>(hackathon);
        }

        public async Task<HackathonResponseDto?> UpdateHackathonAsync(int id, HackathonCreateDto dto, int userId)
        {
            var hackathon = await _uow.Hackathons.GetByIdAsync(id);
            if (hackathon == null)
                return null;

            if (hackathon.CreatedBy != userId)
                throw new UnauthorizedAccessException("You are not authorized to update this hackathon");

            var season = await _uow.Seasons.GetByIdAsync(dto.SeasonId);
            if (season == null)
                throw new ArgumentException("Season not found");

            var seasonStart = DateOnly.FromDateTime(season.StartDate);
            var seasonEnd = DateOnly.FromDateTime(season.EndDate);

            // Rule 1: EndDate phải sau StartDate
            if (dto.EndDate <= dto.StartDate)
                throw new ArgumentException("EndDate must be later than StartDate");

            // Rule 2: Start/End phải nằm trong range Season
            if (dto.StartDate < seasonStart || dto.EndDate > seasonEnd)
                throw new ArgumentException(
                    $"Hackathon dates must fall within the season ({seasonStart:yyyy-MM-dd} - {seasonEnd:yyyy-MM-dd})"
                );
            var phases = await _uow.HackathonPhases.GetAllAsync(p => p.HackathonId == id);

            if (phases.Any())
            {
                // Tìm min - max phase date
                var minPhaseStart = phases.Min(p => p.StartDate);
                var maxPhaseEnd = phases.Max(p => p.EndDate);

                // Phase dùng DateTime? nên convert ra DateOnly
                var minPhaseStartDateOnly = DateOnly.FromDateTime(minPhaseStart.Value);
                var maxPhaseEndDateOnly = DateOnly.FromDateTime(maxPhaseEnd.Value);

                // 1. Nếu user cố đổi ngày mà không bao trọn phases → không cho đổi
                if (dto.StartDate > minPhaseStartDateOnly || dto.EndDate < maxPhaseEndDateOnly)
                {
                    throw new ArgumentException(
                        $"Cannot update Hackathon dates because existing phases ({minPhaseStartDateOnly:yyyy-MM-dd} → {maxPhaseEndDateOnly:yyyy-MM-dd}) " +
                        $"must lie within Hackathon date range."
                    );
                }
            }

            hackathon.Name = dto.Name;
            hackathon.SeasonId = dto.SeasonId;
            hackathon.Description = dto.Description;
            hackathon.StartDate = dto.StartDate;
            hackathon.EndDate = dto.EndDate;
            hackathon.Status = dto.Status ?? hackathon.Status;

            _uow.Hackathons.Update(hackathon);
            await _uow.SaveAsync();

            return _mapper.Map<HackathonResponseDto>(hackathon);
        }



        public async Task<bool> DeleteAsync(int id)
        {
            var hackathon = await _uow.Hackathons.GetByIdAsync(id);
            if (hackathon == null) return false;

            var now = DateTime.UtcNow;

            // Chuyển DateOnly? sang DateTime?
            DateTime? startDate = hackathon.StartDate?.ToDateTime(TimeOnly.MinValue);
            DateTime? endDate = hackathon.EndDate?.ToDateTime(TimeOnly.MaxValue);

            // Validate
            if (startDate.HasValue && now >= startDate.Value)
                throw new InvalidOperationException("Cannot delete a hackathon that has already started or ended.");

            // Nếu chưa bắt đầu → cho phép xóa
            _uow.Hackathons.Remove(hackathon);
            await _uow.SaveAsync();
            return true;
        }


        public async Task<HackathonResponseDto?> UpdateStatusAsync(int id, string status)
        {
            // Validate status hợp lệ
            var validStatuses = new[] { "Pending", "InProgress", "Complete", "Unactive" };
            if (!validStatuses.Contains(status))
                throw new ArgumentException("Invalid status");

            var hackathon = await _uow.Hackathons.GetByIdAsync(id);
            if (hackathon == null)
                return null;

            // (Optionally) validate transition rules here

            hackathon.Status = status;
            _uow.Hackathons.Update(hackathon);
            await _uow.SaveAsync();

            return _mapper.Map<HackathonResponseDto>(hackathon);
        }
        public async Task<IEnumerable<HackathonResponseDto>> GetBySeasonAsync(int seasonId)
        {
            var entities = await _uow.Hackathons.GetAllIncludingAsync(
                h => h.SeasonId == seasonId,
                h => h.Season     // MUST include this
            );

            return _mapper.Map<IEnumerable<HackathonResponseDto>>(entities);
        }


    }
}
