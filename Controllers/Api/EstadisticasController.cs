using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Gaming.Services;

namespace Proyecto_Gaming.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class EstadisticasController : ControllerBase
    {
        private readonly IStatsService _stats;

        public EstadisticasController(IStatsService stats)
        {
            _stats = stats;
        }

        // GET: api/Estadisticas/user/{userId}
        [HttpGet("user/{userId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetUserStats(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return BadRequest("userId is required");

            var stats = await _stats.GetUserStatsAsync(userId);
            return Ok(stats);
        }
    }
}
