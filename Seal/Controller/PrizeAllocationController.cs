using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;

namespace Seal.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class PrizeAllocationController : ControllerBase
    {
        private readonly IPrizeAllocationService _service;

        public PrizeAllocationController(IPrizeAllocationService service)
        {
            _service = service;
        }

        [HttpPost("auto-allocates/{hackathonId}")]
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> AutoAllocates(int hackathonId)
        {
            var result = await _service.AutoAllocatePrizesAsyncs(hackathonId);

            return Ok(result); 
        }


        [HttpGet("hackathon/{hackathonId}")]
        [Authorize]
        public async Task<IActionResult> GetByHackathon(int hackathonId)
        {
            var result = await _service.GetPrizeAllocationsByHackathonAsync(hackathonId);
            return Ok(result);
        }

    }

}
