using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Proyecto_Gaming.Data;
using Proyecto_Gaming.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Proyecto_Gaming.Controllers
{
    public class BibliotecaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly string _connectionString;

        public BibliotecaController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // GET: Biblioteca
        public async Task<IActionResult> Index(string categoria, string plataforma, string busqueda)
        {
            // Verificar si el usuario está autenticado (comprobar si hay un UserId en la sesión)
            var userId = HttpContext.Session.GetString("UserId");

            if (userId == null)
            {
                // Si el usuario no está autenticado, redirigirlo al login
                TempData["Error"] = "Debes iniciar sesión para acceder al catálogo.";
                return RedirectToAction("Login", "Account");
            }

            var juegos = new List<Juego>();
            var categorias = new List<string>();
            var plataformas = new List<string>();

            await using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Query base
                var query = "SELECT id_juego, nombre, categoria, plataforma, imagen, puntuacion_media FROM Juegos WHERE 1=1";
                var parameters = new List<NpgsqlParameter>();

                if (!string.IsNullOrWhiteSpace(categoria) && categoria != "Todas")
                {
                    query += " AND categoria = @Categoria";
                    parameters.Add(new NpgsqlParameter("@Categoria", categoria));
                }

                if (!string.IsNullOrWhiteSpace(plataforma) && plataforma != "Todas")
                {
                    query += " AND plataforma = @Plataforma";
                    parameters.Add(new NpgsqlParameter("@Plataforma", plataforma));
                }

                if (!string.IsNullOrWhiteSpace(busqueda))
                {
                    query += " AND nombre ILIKE @Busqueda";
                    parameters.Add(new NpgsqlParameter("@Busqueda", $"%{busqueda}%"));
                }

                // Ejecutar query de juegos
                await using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddRange(parameters.ToArray());

                    await using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            juegos.Add(new Juego
                            {
                                IdJuego = reader.GetInt32(0),
                                Nombre = reader.GetString(1),
                                Categoria = reader.GetString(2),
                                Plataforma = reader.GetString(3),
                                Imagen = reader.IsDBNull(4) ? null : reader.GetString(4),
                                PuntuacionMedia = reader.IsDBNull(5) ? (decimal?)null : reader.GetDecimal(5)
                            });
                        }
                    }
                }

                // Obtener categorías únicas
                await using (var catCmd = new NpgsqlCommand("SELECT DISTINCT categoria FROM Juegos ORDER BY categoria", connection))
                await using (var catReader = await catCmd.ExecuteReaderAsync())
                {
                    while (await catReader.ReadAsync())
                    {
                        categorias.Add(catReader.GetString(0));
                    }
                }

                // Obtener plataformas únicas
                await using (var platCmd = new NpgsqlCommand("SELECT DISTINCT plataforma FROM Juegos ORDER BY plataforma", connection))
                await using (var platReader = await platCmd.ExecuteReaderAsync())
                {
                    while (await platReader.ReadAsync())
                    {
                        plataformas.Add(platReader.GetString(0));
                    }
                }
            }

            // Pasar las categorías, plataformas y juegos a la vista
            ViewBag.Categorias = categorias;
            ViewBag.Plataformas = plataformas;
            ViewBag.CategoriaSeleccionada = categoria;
            ViewBag.PlataformaSeleccionada = plataforma;
            ViewBag.Busqueda = busqueda;

            return View(juegos);
        }

        public async Task<IActionResult> AddToLibrary(int id)
        {
            // Verificar si el usuario está autenticado (comprobar si hay un UserId en la sesión)
            var userId = HttpContext.Session.GetString("UserId");

            if (userId == null)
            {
                TempData["Error"] = "Debes iniciar sesión para añadir juegos a tu biblioteca.";
                return RedirectToAction("Login", "Account");
            }

            // Cargar entidades necesarias
            var juego = await _context.Juegos.FindAsync(id);
            if (juego == null)
            {
                TempData["Error"] = "El juego no existe.";
                return RedirectToAction(nameof(Index));
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.id_usuario.ToString() == userId);

            if (usuario == null)
            {
                TempData["Error"] = "No se pudo identificar al usuario.";
                return RedirectToAction(nameof(Index));
            }

            // Evitar duplicados en la biblioteca
            var existente = await _context.BibliotecaUsuario
                .FirstOrDefaultAsync(b => b.IdUsuario == usuario.id_usuario && b.IdJuego == juego.IdJuego);

            if (existente == null)
            {
                _context.BibliotecaUsuario.Add(new BibliotecaUsuario
                {
                    IdUsuario = usuario.id_usuario,
                    IdJuego = juego.IdJuego,
                    Estado = "Pendiente"
                });

                await _context.SaveChangesAsync();
                TempData["Ok"] = "Juego añadido a Pendientes.";
            }
            else
            {
                if (!string.Equals(existente.Estado, "Pendiente"))
                {
                    existente.Estado = "Pendiente";
                    _context.Update(existente);
                    await _context.SaveChangesAsync();
                    TempData["Ok"] = "El juego ahora está en Pendientes.";
                }
                else
                {
                    TempData["Ok"] = "Este juego ya está en tus Pendientes.";
                }
            }

            return RedirectToAction(nameof(Pendientes));
        }

        // GET: Biblioteca/Pendientes
        public async Task<IActionResult> Pendientes()
        {
            // Verificar si el usuario está autenticado
            var userId = HttpContext.Session.GetString("UserId");

            if (userId == null)
            {
                TempData["Error"] = "Debes iniciar sesión para ver tus juegos pendientes.";
                return RedirectToAction("Login", "Account");
            }

            // Obtener juegos pendientes del usuario
            var juegosPendientes = new List<Juego>();

            // Esto puede ser reemplazado por la base de datos, en este caso está solo como ejemplo.
            juegosPendientes.Add(new Juego
            {
                IdJuego = 1,
                Nombre = "Juego Ejemplo 1",
                Categoria = "Acción",
                Plataforma = "PC",
                Imagen = "image_url_1.jpg",
                PuntuacionMedia = 4.5m
            });

            juegosPendientes.Add(new Juego
            {
                IdJuego = 2,
                Nombre = "Juego Ejemplo 2",
                Categoria = "Aventura",
                Plataforma = "PlayStation",
                Imagen = "image_url_2.jpg",
                PuntuacionMedia = 4.0m
            });

            // Pasar los juegos a la vista
            return View(juegosPendientes); // Ver los juegos pendientes de la biblioteca
        }

        // POST/GET: Biblioteca/MarkAsPlaying/5
        public async Task<IActionResult> MarkAsPlaying(int id)
        {
            var userId = HttpContext.Session.GetString("UserId");

            if (userId == null)
            {
                TempData["Error"] = "Debes iniciar sesión.";
                return RedirectToAction(nameof(Pendientes));
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.id_usuario.ToString() == userId);

            if (usuario == null)
            {
                TempData["Error"] = "No se pudo identificar al usuario.";
                return RedirectToAction(nameof(Pendientes));
            }

            var biblioteca = await _context.BibliotecaUsuario
                .FirstOrDefaultAsync(b => b.IdUsuario == usuario.id_usuario &&
                                          b.IdJuego == id &&
                                          b.Estado == "Pendiente");

            if (biblioteca == null)
            {
                TempData["Error"] = "No se encontró el juego en Pendientes.";
                return RedirectToAction(nameof(Pendientes));
            }

            biblioteca.Estado = "Jugando";
            _context.Update(biblioteca);
            await _context.SaveChangesAsync();

            TempData["Ok"] = "¡Disfruta! Marcado como 'Jugando'.";
            return RedirectToAction(nameof(Pendientes));
        }

        // GET: Biblioteca/Detalles/5 (detalle del juego con Npgsql)
        public async Task<IActionResult> Detalles(int? id)
        {
            if (id == null) return NotFound();

            Juego juego = null;

            await using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = "SELECT id_juego, nombre, categoria, plataforma, imagen, puntuacion_media FROM Juegos WHERE id_juego = @Id";
                await using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id.Value);

                    await using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            juego = new Juego
                            {
                                IdJuego = reader.GetInt32(0),
                                Nombre = reader.GetString(1),
                                Categoria = reader.GetString(2),
                                Plataforma = reader.GetString(3),
                                Imagen = reader.IsDBNull(4) ? null : reader.GetString(4),
                                PuntuacionMedia = reader.IsDBNull(5) ? (decimal?)null : reader.GetDecimal(5)
                            };
                        }
                    }
                }
            }

            if (juego == null) return NotFound();
            return View(juego);
        }
    }
}
