using AutoMapper;
using Common.DTOs.ChallengeDto;
using Microsoft.AspNetCore.Http;
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
    public class ChallengeService : IChallengeService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;
        private readonly IFileUploadService _fileUploadService;

        public ChallengeService(IUOW uow, IMapper mapper, IFileUploadService fileUploadService)
        {
            _uow = uow;
            _mapper = mapper;
            _fileUploadService = fileUploadService;
        }

        public async Task<IEnumerable<ChallengeDto>> GetAllAsync()
        {
            var entities = await _uow.ChallengeRepository.GetAllIncludingAsync();
            return _mapper.Map<IEnumerable<ChallengeDto>>(entities);
        }

        public async Task<ChallengeDto?> GetByIdAsync(int id)
        {
            var entity = await _uow.ChallengeRepository.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<ChallengeDto>(entity);
        }

        public async Task<ChallengeDto> CreateAsync(ChallengeCreateUnifiedDto dto, int userId)
        {
            // ✅ Validate: file is required
            if (dto.File == null)
                throw new ArgumentException("You must provide a file");

            string? fileUrl = null;

            if (dto.File != null)
            {
                // Upload file lên Cloudinary
                fileUrl = await _fileUploadService.UploadAsync(dto.File);
            }
            var challenge = new Challenge
            {
                Title = dto.Title,
                Description = dto.Description,
                HackathonId = dto.HackathonId,
                UserId = userId,
                FilePath = fileUrl,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            await _uow.ChallengeRepository.AddAsync(challenge);
            await _uow.SaveAsync();

            return _mapper.Map<ChallengeDto>(challenge);
        }


        public async Task<string?> PartnerDeleteAsync(int id, int userId)
        {
            var entity = await _uow.ChallengeRepository.GetByIdAsync(id);
            if (entity == null)
                return "Challenge not found";

            if (entity.UserId != userId)
                return "You do not have permission to delete this challenge";

            // Cannot delete if Complete or Cancel
            if (entity.Status == "Complete" || entity.Status == "Cancel")
                return $"Cannot delete challenge with status '{entity.Status}'";

            _uow.ChallengeRepository.Remove(entity);
            await _uow.SaveAsync();
            return null; // Success
        }


        public async Task<bool> ChangeStatusAsync(int id, ChallengeStatusDto statusDto)
        {
            var entity = await _uow.ChallengeRepository.GetByIdAsync(id);
            if (entity == null) return false;

            entity.Status = statusDto.Status;
            _uow.ChallengeRepository.Update(entity);
            await _uow.SaveAsync();
            return true;
        }

        public async Task<string?> PartnerUpdateAsync(int id, int userId, ChallengePartnerUpdateDto dto)
        {
            var entity = await _uow.ChallengeRepository.GetByIdAsync(id);
            if (entity == null)
                return "Challenge not found";

            if (entity.UserId != userId)
                return "You are not the owner of this challenge";

            if (entity.Status == "Complete" || entity.Status == "Cancel")
                return "Challenge is Complete or Cancelled, cannot update";

            entity.Title = dto.Title;
            entity.Description = dto.Description;
            entity.HackathonId = dto.HackathonId;

            // ✅ Upload new file (if provided)
            if (dto.File != null)
            {
                var fileUrl = await _fileUploadService.UploadAsync(dto.File);
                entity.FilePath = fileUrl;
            }

            // ✅ Validate: file is required
            if (string.IsNullOrEmpty(entity.FilePath))
                return "Challenge must have a file";

            _uow.ChallengeRepository.Update(entity);
            await _uow.SaveAsync();

            return null;
        }

        public async Task<List<ChallengeDto>> GetCompletedChallengesByHackathonAsync(int hackathonId)
        {
            // Lấy toàn bộ challenge completed theo hackathon
            var challenges = await _uow.ChallengeRepository
                .GetCompletedChallengesByHackathonIdAsync(hackathonId);

            if (challenges == null || !challenges.Any())
                return new List<ChallengeDto>();

            // Challenge đã được dùng (TrackId != null)
            var filtered = challenges
                .Where(c => c.TrackId == null)
                .ToList();

            return _mapper.Map<List<ChallengeDto>>(filtered);
        }

        public async Task<List<ChallengeDto>> GetChallengesByTrackIdAsync(int trackId)
        {
            var challenges = await _uow.ChallengeRepository.GetAllIncludingAsync(
                c => c.TrackId == trackId,
                c => c.User
            );

            if (challenges == null || !challenges.Any())
                return new List<ChallengeDto>();

            return _mapper.Map<List<ChallengeDto>>(challenges);
        }
        public async Task<List<ChallengeDto>> GetMyChallengesByHackathonAsync(int userId, int hackathonId)
        {
            var challenges = await _uow.ChallengeRepository.GetAllIncludingAsync(
                c => c.UserId == userId && c.HackathonId == hackathonId,
                c => c.User
            );

            return _mapper.Map<List<ChallengeDto>>(challenges);
        }

    }
}
