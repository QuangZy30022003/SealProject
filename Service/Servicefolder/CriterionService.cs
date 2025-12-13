using AutoMapper;
using Common.DTOs.CriterionDTO;
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
    public class CriterionService : ICriterionService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;

        public CriterionService(IUOW uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }


        // Create

        public async Task<List<CriterionResponseDto>> CreateAsync(CriterionCreateDto dto)
        {
            // ✅ Kiểm tra Phase
            var phaseExists = await _uow.HackathonPhases.ExistsAsync(p => p.PhaseId == dto.PhaseId);
            if (!phaseExists)
                throw new Exception("Phase not found");


            // ✅ Validate danh sách
            if (dto.Criteria == null || dto.Criteria.Count == 0)
                throw new Exception("At least one criterion is required");

            // ✅ Validate từng item
            foreach (var item in dto.Criteria)
            {
                if (string.IsNullOrWhiteSpace(item.Name))
                    throw new Exception("Criterion name cannot be empty");

                if (item.Weight <= 0)
                    throw new Exception("Weight must be greater than 0");
            }

            // ✅ Tạo danh sách Criterion
            var createdCriteria = new List<Criterion>();

            foreach (var item in dto.Criteria)
            {
                var criterion = new Criterion
                {
                    PhaseId = dto.PhaseId,
                    Name = item.Name,
                    Weight = item.Weight
                };
                await _uow.Criteria.AddAsync(criterion);
                createdCriteria.Add(criterion);
            }

            await _uow.SaveAsync();

            return _mapper.Map<List<CriterionResponseDto>>(createdCriteria);
        }


        // Get all
        public async Task<List<CriterionResponseDto>> GetAllAsync(int? phaseId = null)
        {
            var criteria = await _uow.Criteria.GetAllIncludingAsync(
                c => !phaseId.HasValue || c.PhaseId == phaseId.Value,
                c => c.Phase
            );

            return _mapper.Map<List<CriterionResponseDto>>(criteria);
        }

        // Get by Id
        public async Task<CriterionResponseDto?> GetByIdAsync(int id)
        {
            var criterion = await _uow.Criteria.GetByIdIncludingAsync(
                c => c.CriteriaId == id,
                c => c.Phase
            );

            if (criterion == null) return null;
            return _mapper.Map<CriterionResponseDto>(criterion);
        }

        // Update
        public async Task<CriterionResponseDto?> UpdateAsync(int id, CriterionUpdateDto dto)
        {
            var criterion = await _uow.Criteria.GetByIdAsync(id);
            if (criterion == null) return null;

            // Validate Track nếu TrackId != null
        

            criterion.Name = dto.Name;
            criterion.Weight = dto.Weight;

            _uow.Criteria.Update(criterion);
            await _uow.SaveAsync();

            return _mapper.Map<CriterionResponseDto>(criterion);
        }

        // Delete
        public async Task<bool> DeleteAsync(int id)
        {
            var criterion = await _uow.Criteria.GetByIdAsync(id);
            if (criterion == null) return false;

            _uow.Criteria.Remove(criterion);
            await _uow.SaveAsync();
            return true;
        }
    }
}