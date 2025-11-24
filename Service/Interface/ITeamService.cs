using Common.DTOs.TeamDto;
using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface ITeamService
    {
        Task<TeamDto> CreateTeamAsync(CreateTeamDto dto, int userId);
        Task<TeamDto?> GetByIdAsync(int id);
        Task<IEnumerable<TeamDto>> GetAllAsync();
        Task<TeamDto?> UpdateAsync(int id, UpdateTeamDto dto);
        Task<bool> DeleteAsync(int id, int userId);
        Task<IEnumerable<TeamDto>> GetTeamsByChapterIdAsync(int chapterId);
    }
}
