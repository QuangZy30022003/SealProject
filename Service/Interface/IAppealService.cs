using Common.DTOs.AppealDto;
using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface IAppealService
    {
        Task<AppealResponseDto> CreateAppealAsync(CreateAppealDto dto, int currentUserId);
        Task<IEnumerable<AppealResponseDto>> GetAppealsByTeamAsync(int teamId);
        Task<IEnumerable<AppealResponseDto>> GetAllAppealsAsync();
        Task<AppealResponseDto?> GetAppealByIdAsync(int appealId);
        Task<AppealResponseDto?> ReviewAppealAsync(int appealId, ReviewAppealDto dto, int reviewerUserId);
        Task<IEnumerable<AppealResponseDto>> GetAppealsByPhaseAsync(int phaseId);
        Task<IEnumerable<AppealResponseDto>> GetAppealsByTeamAndPhaseAsync(int teamId, int phaseId);

    }
}
