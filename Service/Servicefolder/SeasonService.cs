using AutoMapper;
using Common.DTOs.SeasonDto;
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
    public class SeasonService : ISeasonService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;

        public SeasonService(IUOW uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<IEnumerable<SeasonResponse>> GetAllSeasonsAsync()
        {
            var seasons = await _uow.SeasonRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<SeasonResponse>>(seasons);
        }

        public async Task<SeasonResponse?> GetByIdAsync(int id)
        {
            var season = await _uow.SeasonRepository.GetByIdAsync(id);
            return _mapper.Map<SeasonResponse>(season);
        }

        public async Task<string> CreateAsync(SeasonRequest dto)
        {
            // Check duplicate Code
            var existCode = await _uow.SeasonRepository
                .FirstOrDefaultAsync(x => x.Code.ToLower() == dto.SeasonCode.ToLower());

            if (existCode != null)
                return $"Season Code '{dto.SeasonCode}' already exists.";

            // Check duplicate Name
            var existName = await _uow.SeasonRepository
                .FirstOrDefaultAsync(x => x.Name.ToLower() == dto.Name.ToLower());

            if (existName != null)
                return $"Season Name '{dto.Name}' already exists.";

            var season = _mapper.Map<Season>(dto);
            await _uow.SeasonRepository.AddAsync(season);
            await _uow.SaveAsync();

            return "Season created successfully!";
        }

        public async Task<string> UpdateAsync(int id, SeasonUpdateDto dto)
        {
            var season = await _uow.SeasonRepository.GetByIdAsync(id);
            if (season == null)
                return $"Season with ID {id} not found.";

            // Validate: StartDate < EndDate
            if (dto.StartDate >= dto.EndDate)
                return "StartDate must be earlier than EndDate.";

            // Check duplicate Code (ignore current season)
            var existCode = await _uow.SeasonRepository
                .FirstOrDefaultAsync(x => x.Code.ToLower() == dto.SeasonCode.ToLower() && x.SeasonId != id);
            if (existCode != null)
                return $"Season Code '{dto.SeasonCode}' already exists.";

            // Check duplicate Name (ignore current season)
            var existName = await _uow.SeasonRepository
                .FirstOrDefaultAsync(x => x.Name.ToLower() == dto.Name.ToLower() && x.SeasonId != id);
            if (existName != null)
                return $"Season Name '{dto.Name}' already exists.";

            // Update fields
            season.Code = dto.SeasonCode;
            season.Name = dto.Name;
            season.StartDate = dto.StartDate;
            season.EndDate = dto.EndDate;

            _uow.SeasonRepository.Update(season);
            await _uow.SaveAsync();

            return "Season updated successfully!";
        }


        public async Task<bool> DeleteAsync(int id)
        {
            var season = await _uow.SeasonRepository.GetByIdAsync(id);
            if (season == null) return false;

            _uow.SeasonRepository.Remove(season);
            await _uow.SaveAsync();
            return true;
        }
    }
}
