using Common.DTOs.StudentVerification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface IStudentVerificationService
    {
        Task SubmitAsync(int userId, string userEmail, StudentVerificationDto dto);

        Task<bool> RejectVerificationByUserIdAsync(int userId);
        Task<bool> ApproveVerificationByUserIdAsync(int userId);
        Task<List<StudentVerificationAdminDto>> GetPendingOrRejectedVerificationsAsync();
    }
}
