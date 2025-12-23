using AutoMapper;
using Common.DTOs.AuditLogDtos;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
    public class AuditLogService : IAuditLogService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;

        public AuditLogService(IUOW uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

            public async Task LogApiAsync(int userId, string action, string endpoint, object requestBody = null, object responseBody = null)
            {
                // Chỉ serialize input/output cần thiết, tránh entity full
                var detailsObj = new
                {
                    Endpoint = endpoint,
                    Request = requestBody,
                    Response = responseBody
                };

                var log = new AuditLog
                {
                    UserId = userId,
                    Action = action,
                    Details = JsonConvert.SerializeObject(detailsObj, Formatting.Indented),
                    CreatedAt = DateTime.UtcNow
                };

                await _uow.AuditLogs.AddAsync(log);
                await _uow.SaveAsync();
            }

        public async Task<List<AuditLogDto>> GetAuditLogsAsync(int? userId = null)
        {
            var logs = await _uow.AuditLogs.GetAllAsync(
                filter: userId.HasValue ? x => x.UserId == userId : null,
                orderBy: q => q.OrderByDescending(x => x.CreatedAt),
                includeProperties: "User"
            );

            var logsDto = logs.Select(log => new AuditLogDto
            {
                LogId = log.LogId,
                UserId = log.UserId,
                UserName = log.User.FullName,
                Action = log.Action,
                CreatedAt = log.CreatedAt,
                Details = log.Details != null
                    ? JsonConvert.DeserializeObject<Dictionary<string, object>>(log.Details)
                    : null
            }).ToList();

            return logsDto;
        }
        public async Task<List<AuditLogDto>> GetAllAuditLogsAsync()
        {
            return await GetAuditLogsAsync(null);
        }

    }

}
