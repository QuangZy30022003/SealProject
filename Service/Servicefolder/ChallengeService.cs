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

        public ChallengeService(IUOW uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ChallengeDto>> GetAllAsync()
        {
            var entities = await _uow.ChallengeRepository.GetChallengesWithSeasonAndUserAsync();
            return _mapper.Map<IEnumerable<ChallengeDto>>(entities);
        }

        public async Task<ChallengeDto?> GetByIdAsync(int id)
        {
            var entity = await _uow.ChallengeRepository.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<ChallengeDto>(entity);
        }

        public async Task<ChallengeDto> CreateFromFileAsync(ChallengeCreateUnifiedDto dto, IFormFile file, int userId)
        {
            // lưu file vào wwwroot/uploads/challenges/
            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "challenges");
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var challenge = new Challenge
            {
                Title = dto.Title,
                Description = dto.Description,
                SeasonId = dto.SeasonId,
                UserId = userId,
                FilePath = $"/uploads/challenges/{fileName}", // đường dẫn lưu DB
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            await _uow.ChallengeRepository.AddAsync(challenge);
            await _uow.SaveAsync();

            return _mapper.Map<ChallengeDto>(challenge);
        }

        public async Task<ChallengeDto> CreateFromLinkAsync(ChallengeCreateUnifiedDto dto, int userId)
        {
            var challenge = new Challenge
            {
                Title = dto.Title,
                Description = dto.Description,
                SeasonId = dto.SeasonId,
                UserId = userId,
                FilePath = dto.FilePath, // link online
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            await _uow.ChallengeRepository.AddAsync(challenge);
            await _uow.SaveAsync();

            return _mapper.Map<ChallengeDto>(challenge);
        }


        public async Task<bool> PartnerDeleteAsync(int id, int userId)
        {
            var entity = await _uow.ChallengeRepository.GetByIdAsync(id);
            if (entity == null) return false;

            if (entity.UserId != userId) return false;

            // Không cho xóa nếu Complete hoặc Cancel
            if (entity.Status == "Complete" || entity.Status == "Cancel")
                return false;

            _uow.ChallengeRepository.Remove(entity);
            await _uow.SaveAsync();
            return true;
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
                return "Challenge không tồn tại";

            // Chỉ cho phép partner update challenge do mình tạo
            if (entity.UserId != userId)
                return "Bạn không phải chủ sở hữu challenge này";

            // Không cho update nếu đã Complete hoặc Cancel
            if (entity.Status == "Complete" || entity.Status == "Cancel")
                return "Challenge đã Complete hoặc Cancel, không thể update";

            entity.Title = dto.Title;
            entity.Description = dto.Description;
            entity.SeasonId = dto.SeasonId;

            // Upload file local
            if (dto.File != null)
            {
                // Xóa file cũ nếu có
                if (!string.IsNullOrEmpty(entity.FilePath) && entity.FilePath.StartsWith("/uploads/"))
                {
                    var oldFile = Path.Combine("wwwroot", entity.FilePath.TrimStart('/'));
                    if (File.Exists(oldFile))
                    {
                        File.Delete(oldFile);
                    }
                }

                // Upload file mới
                var uploadsFolder = Path.Combine("wwwroot", "uploads", "challenges");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}_{dto.File.FileName}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.File.CopyToAsync(stream);
                }

                entity.FilePath = $"/uploads/challenges/{fileName}";
            }
            else if (!string.IsNullOrEmpty(dto.FilePath))
            {
                // Update bằng link online
                entity.FilePath = dto.FilePath;
            }

            _uow.ChallengeRepository.Update(entity);
            await _uow.SaveAsync();

            return null; // null nghĩa là thành công
        }

        //public async Task<List<ChallengeDto>> RandomAssignAsync(int hackathonId, int numberOfPhases)
        //{
        //    // 1. Lấy challenge đã duyệt theo hackathon
        //    var challenges = await _uow.ChallengeRepository.GetApprovedChallengesByHackathonAsync(hackathonId);

        //    if (challenges.Count < numberOfPhases)
        //        throw new Exception("Không đủ đề đã duyệt để phân bổ cho các vòng thi");

        //    // 2. Random chọn đề
        //    var random = new Random();
        //    var selectedChallenges = challenges
        //        .OrderBy(c => random.Next())
        //        .Take(numberOfPhases)
        //        .ToList();

        //    // 3. Lấy phase theo hackathon
        //    var phases = await _uow.HackathonPhaseRepository.GetPhasesByHackathonAsync(hackathonId);

        //    if (phases.Count < numberOfPhases)
        //        throw new Exception("Số phase trong hackathon không đủ");

        //    // 4. Gán challenge vào phase
        //    for (int i = 0; i < numberOfPhases; i++)
        //    {
        //        phases[i].ChallengeId = selectedChallenges[i].ChallengeId;
        //        _uow.HackathonPhaseRepository.Update(phases[i]);
        //    }

        //    await _uow.SaveAsync();

        //    // 5. Map sang DTO trả về
        //    return _mapper.Map<List<ChallengeDto>>(selectedChallenges);
        //}
    }
}
