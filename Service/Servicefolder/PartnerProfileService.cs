using AutoMapper;
using Common.DTOs.PartnerProfileDto;
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
    public class PartnerProfileService : IPartnerProfileService
        {
            private readonly IUOW _uow;
            private readonly IMapper _mapper;
            private readonly IFileUploadService _fileUpload;

            public PartnerProfileService(IUOW uow, IMapper mapper, IFileUploadService fileUpload)
            {
                _uow = uow;
                _mapper = mapper;
                _fileUpload = fileUpload;
            }

        // ===================
        // CREATE
        // ===================
        public async Task<PartnerProfileDto> CreateAsync(int userId, PartnerProfileCreateDto dto)
        {
            // 1) CHECK PARTNER PROFILE EXISTS
            var existingProfile = await _uow.PartnerProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (existingProfile != null)
            {
                throw new Exception("This partner already has a profile and cannot create another one.");
            }

            // 2) UPLOAD LOGO
            string logoUrl = null;

            if (dto.LogoFile != null)
            {
                logoUrl = await _fileUpload.UploadAsync(dto.LogoFile);
            }

            // 3) CREATE NEW PROFILE
            var profile = new PartnerProfile
            {
                UserId = userId,
                CompanyName = dto.CompanyName,
                Description = dto.Description,
                Website = dto.Website,
                LogoUrl = logoUrl
            };

            // 4) SAVE TO DB
            await _uow.PartnerProfiles.AddAsync(profile);
            await _uow.SaveAsync();

            return _mapper.Map<PartnerProfileDto>(profile);
        }
        // ===================
        // UPDATE
        // ===================
        public async Task<PartnerProfileDto> UpdateAsync(int userId, PartnerProfileUpdateDto dto)
            {
                var entity = await _uow.PartnerProfiles.FirstOrDefaultAsync(p => p.UserId == userId);

                if (entity == null)
                    throw new Exception("Profile not found.");

                // Upload logo nếu có file mới
                if (dto.LogoFile != null)
                {
                    entity.LogoUrl = await _fileUpload.UploadAsync(dto.LogoFile);
                }

                // Map các field text
                _mapper.Map(dto, entity);

                _uow.PartnerProfiles.Update(entity);
                await _uow.SaveAsync();

                return _mapper.Map<PartnerProfileDto>(entity);
            }

            // ===================
            // DELETE
            // ===================
            public async Task<bool> DeleteAsync(int userId)
            {
                var entity = await _uow.PartnerProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
                if (entity == null)
                    return false;

                _uow.PartnerProfiles.Remove(entity);
                await _uow.SaveAsync();
                return true;
            }

            // ===================
            // GET ONE
            // ===================
            public async Task<PartnerProfileDto> GetByUserIdAsync(int userId)
            {
                var entity = await _uow.PartnerProfiles.FirstOrDefaultAsync(x => x.UserId == userId);
                return _mapper.Map<PartnerProfileDto>(entity);
            }

            // ===================
            // GET ALL (Admin)
            // ===================
            public async Task<IEnumerable<PartnerProfileDto>> GetAllAsync()
            {
                var list = await _uow.PartnerProfiles.GetAllAsync();
                return _mapper.Map<IEnumerable<PartnerProfileDto>>(list);
            }

        public async Task<PartnerProfileDto> GetMyProfileAsync(int userId)
        {
            var entity = await _uow.PartnerProfiles.FirstOrDefaultAsync(x => x.UserId == userId);

            if (entity == null)
                throw new Exception("You do not have a partner profile yet.");

            return _mapper.Map<PartnerProfileDto>(entity);
        }
        public async Task<PartnerProfileDto> AdminGetByUserIdAsync(int userId)
        {
            var entity = await _uow.PartnerProfiles.FirstOrDefaultAsync(p => p.UserId == userId);

            if (entity == null)
                throw new Exception("Partner profile not found.");

            return _mapper.Map<PartnerProfileDto>(entity);
        }

    }
}
