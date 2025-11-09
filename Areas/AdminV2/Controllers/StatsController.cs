using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Gaming.Areas.AdminV2.Services;

namespace Proyecto_Gaming.Areas.AdminV2.Controllers
{
    [Area("AdminV2")]
    [Authorize] // si quieres solo admins: [Authorize(Roles = "Admin")]
    public class StatsController : Controller
    {
        private readonly IStatsService _svc;
        public StatsController(IStatsService svc) => _svc = svc;

        [HttpGet]
        public async Task<IActionResult> Index(DateTime? from, DateTime? to)
        {
            var vm = await _svc.GetAsync(from, to);
            return View(vm); // Vista: Areas/AdminV2/Views/Stats/Index.cshtml
        }
    }
}
