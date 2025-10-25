using AutoMapper;
using Common.DTOs.PrizeDto;
using Repositories.Interface;
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
    public class PrizeService : IPrizeService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;

        public PrizeService(IUOW uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<IEnumerable<PrizeDTO>> GetAllAsync()
        {
            var prizes = await _uow.PrizeRepository.GetAllIncludingAsync(null, p => p.Hackathon);
            return _mapper.Map<IEnumerable<PrizeDTO>>(prizes);
        }

        public async Task<IEnumerable<PrizeDTO>> GetByHackathonAsync(int hackathonId)
        {
            var prizes = await _uow.PrizeRepository.GetPrizesByHackathonIdAsync(hackathonId);
            return _mapper.Map<IEnumerable<PrizeDTO>>(prizes);
        }

        public async Task<PrizeDTO> CreateAsync(CreatePrizeDTO dto)
        {
            // ✅ Kiểm tra hackathon tồn tại
            var hackathonExists = await _uow.Hackathons.ExistsAsync(h => h.HackathonId == dto.HackathonId);
            if (!hackathonExists)
                throw new Exception("Hackathon không tồn tại.");

            var prize = _mapper.Map<Prize>(dto);
            await _uow.PrizeRepository.AddAsync(prize);
            await _uow.SaveAsync();

            return _mapper.Map<PrizeDTO>(prize);
        }

        public async Task<PrizeDTO> UpdateAsync(UpdatePrizeDTO dto)
        {
            var prize = await _uow.PrizeRepository.GetByIdAsync(dto.PrizeId);
            if (prize == null)
                throw new Exception("Giải thưởng không tồn tại.");

            _mapper.Map(dto, prize);
            _uow.PrizeRepository.Update(prize);
            await _uow.SaveAsync();

            return _mapper.Map<PrizeDTO>(prize);
        }

        public async Task DeleteAsync(int prizeId)
        {
            var prize = await _uow.PrizeRepository.GetByIdAsync(prizeId);
            if (prize == null)
                throw new Exception("Giải thưởng không tồn tại.");

            _uow.PrizeRepository.Remove(prize);
            await _uow.SaveAsync();
        }
    }
}
