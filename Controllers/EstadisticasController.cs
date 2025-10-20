using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Gaming.Models;
using Proyecto_Gaming.Services;
using Proyecto_Gaming.ViewModels;
using System.Collections.Generic;

namespace Proyecto_Gaming.Controllers
{
    [Authorize]
    public class EstadisticasController : Controller
    {
        private readonly IStatsService _statsService;
        private readonly UserManager<Usuario> _userManager;

        public EstadisticasController(IStatsService statsService, UserManager<Usuario> userManager)
        {
            _statsService = statsService;
            _userManager = userManager;
        }

        // GET: /Estadisticas
        public async Task<IActionResult> Index()
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null)
                return RedirectToAction("Login", "Account");

            var stats = await _statsService.GetUserStatsAsync(usuario.Id);
            // Construir ViewModel para la vista de Estadísticas
            var model = new PerfilViewModel
            {
                Usuario = usuario,
                AmigosCount = stats.FriendsCount,
                TotalJuegos = stats.TotalGames,
                TotalHoras = stats.TotalHours,
                JuegosPendientes = stats.PendingGames,
                JuegosJugando = stats.JuegosJugando,
                JuegosCompletados = stats.CompletedGames,
                GruposCount = stats.GroupsCount,
                BibliotecaReciente = new List<BibliotecaUsuario>(),
                JuegosDestacados = new List<BibliotecaUsuario>(),
                AmigosVisual = new List<UsuarioAmigoViewModel>()
            };

            // Pasar datos mensuales al ViewModel si están disponibles
            if (stats.MonthlyHours != null)
            {
                model.MonthlyHours = stats.MonthlyHours;
            }
            // Agregar datos de juegos completados al ViewModel para la gráfica de top juegos
            model.TopJuegosFinalizados = stats.TopCompletedGames;
            // Pasar datos de reseñas mensuales al ViewModel
            if (stats.MonthlyReviews != null)
            {
                model.MonthlyReviews = stats.MonthlyReviews;
            }
            // Asignar conteo total de reseñas realizadas
            model.TotalReviews = stats.ReviewsCount;
            // Pasar datos de nuevos amigos mensuales al ViewModel
            if (stats.MonthlyFriends != null)
            {
                model.MonthlyFriends = stats.MonthlyFriends;
            }
            // Pasar datos de nuevos grupos mensuales al ViewModel
            if (stats.MonthlyGroups != null)
            {
                model.MonthlyGroups = stats.MonthlyGroups;
            }

            return View(model);
        }
    }
}