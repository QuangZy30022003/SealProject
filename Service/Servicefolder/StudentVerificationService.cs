using AutoMapper;
using Common.DTOs.StudentVerification;
using Microsoft.AspNetCore.Hosting;
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
    public class StudentVerificationService : IStudentVerificationService
    {
        private readonly IUOW _uow;
        private readonly IWebHostEnvironment _env;
        private readonly IMapper _mapper;

        public StudentVerificationService(IUOW uow, IWebHostEnvironment env, IMapper mapper)
        {
            _uow = uow;
            _env = env;
            _mapper = mapper;
        }

        public async Task SubmitAsync(int userId, string userEmail, StudentVerificationDto dto)
        {
            var existing = await _uow.StudentVerifications.FirstOrDefaultAsync(sv => sv.UserId == userId);
            if (existing != null)
                throw new Exception("Bạn đã gửi yêu cầu xác thực trước đó.");

            // upload folder
            string uploadDir = Path.Combine(_env.WebRootPath, "uploads/studentCards");
            if (!Directory.Exists(uploadDir))
                Directory.CreateDirectory(uploadDir);

            string? frontPath = null;
            string? backPath = null;

            // Save front image
            if (dto.FrontCardImage != null)
            {
                string fileName = $"{Guid.NewGuid()}_{dto.FrontCardImage.FileName}";
                string path = Path.Combine(uploadDir, fileName);
                using (var stream = new FileStream(path, FileMode.Create))
                    await dto.FrontCardImage.CopyToAsync(stream);
                frontPath = $"/uploads/studentCards/{fileName}";
            }

            // Save back image
            if (dto.BackCardImage != null)
            {
                string fileName = $"{Guid.NewGuid()}_{dto.BackCardImage.FileName}";
                string path = Path.Combine(uploadDir, fileName);
                using (var stream = new FileStream(path, FileMode.Create))
                    await dto.BackCardImage.CopyToAsync(stream);
                backPath = $"/uploads/studentCards/{fileName}";
            }

            // Map từ DTO sang Entity
            var verification = _mapper.Map<StudentVerification>(dto);
            verification.UserId = userId;
            verification.StudentEmail = userEmail;
            verification.FrontCardImage = frontPath;
            verification.BackCardImage = backPath;
            verification.Status = "Pending";

            await _uow.StudentVerifications.AddAsync(verification);
            await _uow.SaveAsync();
        }

        public async Task<bool> ApproveVerificationAsync(int verificationId)
        {
            var verification = await _uow.StudentVerifications.GetByIdAsync(verificationId);
            if (verification == null)
                return false;

            // ✅ Chỉ cho phép duyệt nếu đang Pending hoặc Rejected
            if (verification.Status != "Pending" && verification.Status != "Rejected")
                throw new InvalidOperationException("Chỉ có thể duyệt các yêu cầu đang ở trạng thái Pending hoặc Rejected.");

            verification.Status = "Approved";
            verification.UpdatedAt = DateTime.Now;

            _uow.StudentVerifications.Update(verification);
            await _uow.SaveAsync();

            return true;
        }

        public async Task<bool> RejectVerificationAsync(int verificationId)
        {
            var verification = await _uow.StudentVerifications.GetByIdAsync(verificationId);
            if (verification == null)
                return false;

            // ✅ Không được từ chối nếu đã Approved
            if (verification.Status == "Approved")
                throw new InvalidOperationException("Không thể từ chối yêu cầu đã được duyệt.");

            verification.Status = "Rejected";
            verification.UpdatedAt = DateTime.Now;

            // Nếu có cột lưu lý do
            // verification.RejectionReason = reason;

            _uow.StudentVerifications.Update(verification);
            await _uow.SaveAsync();

            return true;
        }

    }
}
