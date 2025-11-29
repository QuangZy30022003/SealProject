using AutoMapper;
using Common.DTOs.MentorVerificationDto;
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
    public class MentorVerificationService : IMentorVerificationService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;
        private readonly IFileUploadService _fileUploadService;

        public MentorVerificationService(IUOW uow, IMapper mapper, IFileUploadService fileUploadService)
        {
            _uow = uow;
            _mapper = mapper;
            _fileUploadService = fileUploadService;
        }

        public async Task<MentorVerificationResponseDto> CreateAsync(MentorVerificationCreateDto dto, IFormFile cvFile, int userId)
        {
            // Check duplicate pending request
            var exists = await _uow.MentorVerifications.ExistsAsync(x =>
                x.UserId == userId &&
                x.HackathonId == dto.HackathonId &&
                x.Status == "Pending");

            if (exists)
                throw new Exception("You have already sent a mentor request!");

            // Map dto to entity
            var entity = _mapper.Map<MentorVerification>(dto);
            entity.Status = "Pending";
            entity.CreatedAt = DateTime.UtcNow;
            entity.UserId = userId; // Lấy từ JWT

            // Upload CV
            if (cvFile != null)
            {
                var cvUrl = await _fileUploadService.UploadStudnetAsync(cvFile);
                entity.CV = cvUrl;
            }

            // Set ChapterId only, không set ChapterName
            if (dto.ChapterId.HasValue)
            {
                var chapter = await _uow.Chapters.GetByIdAsync(dto.ChapterId.Value);
                if (chapter != null)
                {
                    entity.ChapterId = chapter.ChapterId;
                    // Không lưu ChapterName vào DB
                }
            }

            await _uow.MentorVerifications.AddAsync(entity);
            await _uow.SaveAsync();

            // Mapping Response DTO, lấy ChapterName từ navigation property
            var response = _mapper.Map<MentorVerificationResponseDto>(entity);
            if (entity.Chapter != null)
            {
                response.ChapterName = entity.Chapter.ChapterName; // chỉ để trả về client
            }

            return response;
        }


        public async Task<List<MentorVerificationResponseDto>> GetAllAsync()
        {
            var list = await _uow.MentorVerifications
                .GetAllAsync(includeProperties: "User,Hackathon,Chapter");

            return _mapper.Map<List<MentorVerificationResponseDto>>(list);
        }

        public async Task<MentorVerificationResponseDto?> ApproveAsync(int id, int chapterLeaderId)
        {
            // Lấy entity cùng với User và Chapter
            var entity = await _uow.MentorVerifications.GetByIdIncludingAsync(
                x => x.Id == id,
                x => x.User,
                x => x.Chapter
            );

            if (entity == null) return null;

            // Kiểm tra ChapterLeader
            if (entity.ChapterId.HasValue && entity.Chapter.ChapterLeaderId != chapterLeaderId)
                throw new Exception("You are not authorized to approve this mentor.");

            // Update status
            entity.Status = "Approved";
            entity.RejectReason = null;
            entity.UpdatedAt = DateTime.UtcNow;

            // Cập nhật RoleId của user
            if (entity.User != null)
                entity.User.RoleId = 5; // Mentor

            _uow.MentorVerifications.Update(entity);
            await _uow.SaveAsync();

            return _mapper.Map<MentorVerificationResponseDto>(entity);
        }

        public async Task<MentorVerificationResponseDto?> RejectAsync(int id, string rejectReason, int chapterLeaderId)
        {
            var entity = await _uow.MentorVerifications.GetByIdIncludingAsync(
                x => x.Id == id,
                x => x.User,
                x => x.Chapter
            );

            if (entity == null) return null;

            // Kiểm tra ChapterLeader
            if (entity.ChapterId.HasValue && entity.Chapter.ChapterLeaderId != chapterLeaderId)
                throw new Exception("You are not authorized to reject this mentor.");

            entity.Status = "Reject";
            entity.RejectReason = rejectReason;
            entity.UpdatedAt = DateTime.UtcNow;

            _uow.MentorVerifications.Update(entity);
            await _uow.SaveAsync();

            return _mapper.Map<MentorVerificationResponseDto>(entity);
        }
    }
}