using Common.DTOs.AuditLogDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface IAuditLogService
    {
        Task LogApiAsync(int userId, string action, string endpoint, object requestBody = null, object responseBody = null);
        Task<List<AuditLogDto>> GetAuditLogsAsync(int? userId = null);
        Task<List<AuditLogDto>> GetAllAuditLogsAsync();

    }
}
