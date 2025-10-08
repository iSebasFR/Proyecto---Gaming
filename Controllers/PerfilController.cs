using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Proyecto_Gaming.Models;
using Proyecto_Gaming.ViewModels;
using Proyecto_Gaming.Data;
using System.Diagnostics;

namespace Proyecto_Gaming.Controllers
{
    public class PerfilController : Controller
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public PerfilController(UserManager<Usuario> userManager, 
                              ApplicationDbContext context,
                              IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _context = context;
            _environment = environment;
        }

        // GET: Perfil/Index
        public async Task<IActionResult> Index(string userId = null)
        {
            Usuario usuario;

            if (string.IsNullOrEmpty(userId))
            {
                // Ver perfil propio
                usuario = await _userManager.GetUserAsync(User);
                if (usuario == null)
                {
                    TempData["Error"] = "Debes iniciar sesión para ver tu perfil.";
                    return RedirectToAction("Login", "Account");
                }
            }
            else
            {
                // Ver perfil de otro usuario (para futuro)
                usuario = await _userManager.FindByIdAsync(userId);
                if (usuario == null)
                {
                    return NotFound();
                }
            }

            var viewModel = await ConstruirPerfilViewModel(usuario);
            return View(viewModel);
        }

        private async Task<PerfilViewModel> ConstruirPerfilViewModel(Usuario usuario)
        {
            // Obtener estadísticas de la biblioteca usando SQL directo
            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            var totalJuegos = 0;

            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT COUNT(*) as TotalJuegos
                    FROM ""BibliotecaUsuario"" 
                    WHERE ""IdUsuario"" = @userId";

                var parameter = command.CreateParameter();
                parameter.ParameterName = "userId";
                parameter.Value = usuario.Id;
                command.Parameters.Add(parameter);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        totalJuegos = reader.GetInt32(0);
                    }
                }
            }

            // Obtener biblioteca reciente (últimos 3 juegos)
            var bibliotecaReciente = new List<BibliotecaUsuario>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT * FROM ""BibliotecaUsuario"" 
                    WHERE ""IdUsuario"" = @userId 
                    ORDER BY ""Id"" DESC 
                    LIMIT 3";

                var parameter = command.CreateParameter();
                parameter.ParameterName = "userId";
                parameter.Value = usuario.Id;
                command.Parameters.Add(parameter);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        bibliotecaReciente.Add(new BibliotecaUsuario
                        {
                            Id = reader.GetInt32(0),
                            IdUsuario = reader.GetString(1),
                            RawgGameId = reader.GetInt32(2),
                            Estado = reader.GetString(3),
                            GameName = reader.GetString(4),
                            GameImage = reader.GetString(5),
                            Resena = reader.GetString(6),
                            Calificacion = reader.GetInt32(7),
                            FechaCompletado = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
                            FechaResena = reader.IsDBNull(9) ? null : reader.GetDateTime(9)
                        });
                    }
                }
            }

            await connection.CloseAsync();

            // Datos visuales para amigos (no funcional aún)
            var amigosVisual = new List<UsuarioAmigoViewModel>
            {
                new UsuarioAmigoViewModel { Nombre = "ProGamer99", Estado = "En Línea", Avatar = "PG" },
                new UsuarioAmigoViewModel { Nombre = "NubMaster", Estado = "Ausente", Avatar = "NM" },
                new UsuarioAmigoViewModel { Nombre = "GameLover", Estado = "Jugando", Avatar = "GL" }
            };

            return new PerfilViewModel
            {
                Usuario = usuario,
                TotalJuegos = totalJuegos,
                BibliotecaReciente = bibliotecaReciente,
                AmigosVisual = amigosVisual,
                JuegosDestacados = bibliotecaReciente.Take(2).ToList()
            };
        }
    }
}