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

        public async Task<IEnumerable<CriterionReadDTO>> GetByPhaseChallengeAsync(int phaseChallengeId)
        {
            var entities = await _uow.Criteria.GetAllIncludingAsync(
                c => c.PhaseChallengeId == phaseChallengeId,
                c => c.PhaseChallenge,
                c => c.CriterionDetails);

            return _mapper.Map<IEnumerable<CriterionReadDTO>>(entities);
        }

        public async Task<CriterionReadDTO?> GetByIdAsync(int id)
        {
            var entity = await _uow.Criteria.GetAllIncludingAsync(
                c => c.CriteriaId == id,
                c => c.PhaseChallenge,
                c => c.CriterionDetails);

            var criterion = entity.FirstOrDefault();
            return criterion == null ? null : _mapper.Map<CriterionReadDTO>(criterion);
        }


        public async Task<CriterionReadDTO> CreateAsync(CriterionCreateDTO dto)
        {
            var entity = _mapper.Map<Criterion>(dto);

            if (dto.Details != null && dto.Details.Any())
            {
                entity.CriterionDetails = dto.Details.Select(d => new CriterionDetail
                {
                    Description = d.Description,
                    MaxScore = d.MaxScore
                }).ToList();
            }

            await _uow.Criteria.AddAsync(entity);
            await _uow.SaveAsync();

            var created = await _uow.Criteria.GetAllIncludingAsync(
                c => c.CriteriaId == entity.CriteriaId,
                c => c.PhaseChallenge,
                c => c.CriterionDetails);

            return _mapper.Map<CriterionReadDTO>(created.First());
        }

        public async Task<bool> UpdateAsync(CriterionUpdateDTO dto)
        {
            var entity = await _uow.Criteria.GetAllIncludingAsync(
                c => c.CriteriaId == dto.CriteriaId,
                c => c.CriterionDetails);

            var criterion = entity.FirstOrDefault();
            if (criterion == null) return false;

            // Update main fields
            criterion.Name = dto.Name;
            criterion.Weight = dto.Weight;

            // Replace old details with new
            if (dto.Details != null)
            {
                criterion.CriterionDetails.Clear();
                foreach (var detail in dto.Details)
                {
                    criterion.CriterionDetails.Add(new CriterionDetail
                    {
                        Description = detail.Description,
                        MaxScore = detail.MaxScore
                    });
                }
            }

            _uow.Criteria.Update(criterion);
            await _uow.SaveAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _uow.Criteria.GetByIdAsync(id);
            if (entity == null) return false;

            _uow.Criteria.Remove(entity);
            await _uow.SaveAsync();
            return true;
        }

        public async Task<IEnumerable<CriterionReadDTO>> GetAllAsync()
        {
            var entities = await _uow.Criteria.GetAllIncludingAsync(
                c => true,
                c => c.PhaseChallenge,
                c => c.CriterionDetails);

            return _mapper.Map<IEnumerable<CriterionReadDTO>>(entities);
        }
    }
}
