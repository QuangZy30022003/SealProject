using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repositories.UnitOfWork;
using Service.Interface;

namespace Seal.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuditLogController : ControllerBase
    {
        private readonly IAuditLogService _auditLogService;

        public AuditLogController(IAuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }

        // GET: api/AuditLog?userId=1
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAuditLogs([FromQuery] int? userId)
        {
            var logsDto = await _auditLogService.GetAuditLogsAsync(userId);
            return Ok(logsDto);
        }
    }

    }
