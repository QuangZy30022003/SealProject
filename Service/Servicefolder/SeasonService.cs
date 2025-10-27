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
            // ✅ 1. Lấy entity hiện tại
            var season = await _uow.SeasonRepository.GetByIdAsync(id);
            if (season == null)
                return $"Season with ID {id} not found.";

            // ✅ 2. Validate StartDate < EndDate
            if (dto.StartDate >= dto.EndDate)
                return "StartDate must be earlier than EndDate.";

            // ✅ 3. Kiểm tra trùng Code
            var existCode = await _uow.SeasonRepository.GetByCodeAsync(dto.SeasonCode);
            if (existCode != null && existCode.SeasonId != id)
                return $"Season Code '{dto.SeasonCode}' already exists.";

            // ✅ 4. Kiểm tra trùng Name
            var existName = await _uow.SeasonRepository.GetByNameAsync(dto.Name);
            if (existName != null && existName.SeasonId != id)
                return $"Season Name '{dto.Name}' already exists.";

            // ✅ 5. Mapping DTO → entity (AutoMapper sẽ tự map)
            _mapper.Map(dto, season);

            // ✅ 6. Lưu thay đổi
            await _uow.SeasonRepository.UpdateSeasonAsync(season);

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
