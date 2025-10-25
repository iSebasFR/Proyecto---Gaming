using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto_Gaming.Data;

namespace Proyecto_Gaming.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public HealthController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                // Verificar que la base de datos responde
                var canConnect = await _context.Database.CanConnectAsync();
                
                return Ok(new { 
                    status = "Healthy", 
                    timestamp = DateTime.UtcNow,
                    database = canConnect ? "Connected" : "Disconnected",
                    message = "Gaming API is running"
                });
            }
            catch (Exception ex)
            {
                // Si hay error, devolver status unhealthy
                return StatusCode(500, new { 
                    status = "Unhealthy", 
                    timestamp = DateTime.UtcNow,
                    error = ex.Message,
                    message = "Gaming API has issues"
                });
            }
        }
    }
}