using Common.DTOs.HackathonDto;
using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface IHackathonService
    {
        Task<IEnumerable<HackathonResponseDto>> GetAllAsync();
        Task<HackathonResponseDto?> GetByIdAsync(int id);
        Task<HackathonResponseDto> CreateHackathonAsync(HackathonCreateDto dto, int userId);
        Task<HackathonResponseDto?> UpdateHackathonAsync(int id, HackathonCreateDto dto, int userId);
        Task<bool> DeleteAsync(int id);
        Task<HackathonResponseDto?> UpdateStatusAsync(int id, string status);
    }
}
