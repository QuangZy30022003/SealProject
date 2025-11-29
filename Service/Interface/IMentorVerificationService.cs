using Common.DTOs.MentorVerificationDto;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface IMentorVerificationService
    {
        Task<MentorVerificationResponseDto> CreateAsync(MentorVerificationCreateDto dto, IFormFile cvFile, int userId);
        Task<List<MentorVerificationResponseDto>> GetAllAsync();
        Task<MentorVerificationResponseDto?> ApproveAsync(int id, int userId);
        Task<MentorVerificationResponseDto?> RejectAsync(int id, string rejectReason, int userId);
        Task<List<MentorVerificationResponseDto>> GetApprovedMentorsByHackathonAsync(int hackathonId);
    }


}
