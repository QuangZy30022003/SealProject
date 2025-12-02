    using AutoMapper;
    using CloudinaryDotNet;
    using CloudinaryDotNet.Actions;
    using Common.DTOs.NotificationDto;
    using Common.DTOs.StudentVerification;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
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
            private readonly IMapper _mapper;
            private readonly IFileUploadService _fileUploadService;
            private readonly INotificationService _notificationService;

            public StudentVerificationService(IUOW uow, IMapper mapper, IConfiguration config, IFileUploadService fileUploadService, INotificationService notificationService)
            {
                _uow = uow;
                _mapper = mapper;
                _fileUploadService = fileUploadService;
                _notificationService = notificationService;
            }

            public async Task SubmitAsync(int userId, string userEmail, StudentVerificationDto dto)
            {
                var existing = await _uow.StudentVerifications.FirstOrDefaultAsync(sv => sv.UserId == userId);
                if (existing != null)
                    throw new Exception("Bạn đã gửi yêu cầu xác thực trước đó.");

                string? frontUrl = null;
                string? backUrl = null;

                if (dto.FrontCardImage != null)
                    frontUrl = await _fileUploadService.UploadStudnetAsync(dto.FrontCardImage);

                if (dto.BackCardImage != null)
                    backUrl = await _fileUploadService.UploadStudnetAsync(dto.BackCardImage);

                var verification = _mapper.Map<StudentVerification>(dto);
                verification.UserId = userId;
                verification.StudentEmail = userEmail;
                verification.FrontCardImage = frontUrl;
                verification.BackCardImage = backUrl;
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

                // ✅ GỬI NOTIFICATION
                await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                {
                    UserId = verification.UserId,
                    Message = "Your student verification has been approved! You can now create teams."
                });

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

                // ✅ GỬI NOTIFICATION
                await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                {
                    UserId = verification.UserId,
                    Message = "Your student verification has been rejected. Please resubmit with correct documents."
                });

                return true;
            }
        public async Task<List<StudentVerificationAdminDto>> GetPendingOrRejectedVerificationsAsync()
        {
            // Lấy tất cả yêu cầu có status Pending hoặc Rejected
            var verifications = await _uow.StudentVerifications.GetAllAsync(
                sv => sv.Status == "Pending" || sv.Status == "Rejected"
            );

            // Map sang DTO cho admin
            var dtoList = _mapper.Map<List<StudentVerificationAdminDto>>(verifications);

            return dtoList;
        }
    }
}
