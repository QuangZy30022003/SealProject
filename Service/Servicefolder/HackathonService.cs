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
            var entities = await _uow.Hackathons.GetAllAsync();
            return _mapper.Map<IEnumerable<HackathonResponseDto>>(entities);
        }

        public async Task<HackathonResponseDto?> GetByIdAsync(int id)
        {
            var entity = await _uow.Hackathons.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<HackathonResponseDto>(entity);
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
                Season = season.Name,   // Lưu Name (ex: "Summer 2025")
                Theme = dto.Theme,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                CreatedBy = userId
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
            hackathon.Season = season.Name;
            hackathon.Theme = dto.Theme;
            hackathon.StartDate = dto.StartDate;
            hackathon.EndDate = dto.EndDate;

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
    }
}
