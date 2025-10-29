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
                  h => h.SeasonNavigation);

            return _mapper.Map<IEnumerable<HackathonResponseDto>>(entities);
        }

        public async Task<HackathonDetailResponseDto?> GetByIdAsync(int id)
        {
            var entity = await _uow.HackathonPhaseRepository.GetHackathonDetailAsync(id);
            if (entity == null) return null;

            return _mapper.Map<HackathonDetailResponseDto>(entity);
        }



        public async Task<HackathonResponseDto> CreateHackathonAsync(HackathonCreateDto dto, int userId)
        {
            var season = await _uow.SeasonRepository.GetByIdAsync(dto.SeasonId);
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
                Theme = dto.Theme,
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

            var season = await _uow.SeasonRepository.GetByIdAsync(dto.SeasonId);
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


            hackathon.Name = dto.Name;
            hackathon.SeasonId = dto.SeasonId;
            hackathon.Theme = dto.Theme;
            hackathon.StartDate = dto.StartDate;
            hackathon.EndDate = dto.EndDate;
            hackathon.Status = dto.Status ?? hackathon.Status;

            _uow.Hackathons.Update(hackathon);
            await _uow.SaveAsync();

            return _mapper.Map<HackathonResponseDto>(hackathon);
        }



        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _uow.Hackathons.GetByIdAsync(id);
            if (entity == null) return false;

            _uow.Hackathons.Remove(entity);
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

    }
}
