using Microsoft.EntityFrameworkCore;
using Proyecto_Gaming.ML.Models;
using Proyecto_Gaming.Models.Rawg;
using Proyecto_Gaming.Services;
using Proyecto_Gaming.Data;

namespace Proyecto_Gaming.ML.Services
{
    public class BibliotecaMLService : IBibliotecaMLService
    {
        private readonly ApplicationDbContext _context;
        private readonly IRawgService _rawgService;

        public BibliotecaMLService(ApplicationDbContext context, IRawgService rawgService)
        {
            _context = context;
            _rawgService = rawgService;
        }

        public async Task<List<Game>> GetPersonalizedRecommendationsAsync(string userId)
        {
            Console.WriteLine($"🎯 ML: Generando recomendaciones para usuario {userId}...");

            try
            {
                // 1. ANALIZAR PERFIL DEL USUARIO DESDE TU BD
                var userProfile = await AnalyzeUserProfileAsync(userId);
                
                // 2. OBTENER JUEGOS POPULARES COMO BASE
                var popularGames = await GetPopularGamesAsync(40);
                
                if (!popularGames.Any())
                {
                    Console.WriteLine("⚠️ No hay juegos populares disponibles");
                    return new List<Game>();
                }

                // 3. CALCULAR RECOMENDACIONES PERSONALIZADAS
                var recommendations = await CalculateRecommendationsAsync(popularGames, userProfile);
                
                Console.WriteLine($"✅ ML: {recommendations.Count} recomendaciones generadas para {userId}");
                return recommendations;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR ML: {ex.Message}");
                return await GetPopularGamesAsync(12); // Fallback
            }
        }

        private async Task<UserProfile> AnalyzeUserProfileAsync(string userId)
        {
            var profile = new UserProfile { UserId = userId };

            // OBTENER BIBLIOTECA DEL USUARIO DESDE TU BD
            var userLibrary = await _context.BibliotecaUsuario
                .Where(b => b.UsuarioId == userId)
                .ToListAsync();

            Console.WriteLine($"📊 ML: Analizando biblioteca de {userLibrary.Count} juegos");

            // ANALIZAR CADA JUEGO EN LA BIBLIOTECA
            foreach (var item in userLibrary)
            {
                try
                {
                    var gameDetails = await _rawgService.GetGameDetailsAsync(item.RawgGameId);
                    
                    if (gameDetails?.Genres != null)
                    {
                        // CONTAR GÉNEROS PREFERIDOS
                        foreach (var genre in gameDetails.Genres)
                        {
                            var genreName = genre.Name ?? "Unknown";
                            if (profile.PreferredGenres.ContainsKey(genreName))
                                profile.PreferredGenres[genreName]++;
                            else
                                profile.PreferredGenres[genreName] = 1;
                        }
                    }

                    // REGISTRAR JUEGOS CALIFICADOS
                    if (item.Calificacion > 0)
                    {
                        profile.RatedGames.Add(item.RawgGameId);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ ML: Error analizando juego {item.RawgGameId}: {ex.Message}");
                }
            }

            profile.TotalGames = userLibrary.Count;
            profile.TopGenre = profile.GetTopGenre();

            Console.WriteLine($"📊 ML: Perfil - {profile.TotalGames} juegos, Género top: {profile.TopGenre}");
            return profile;
        }

        private async Task<List<Game>> CalculateRecommendationsAsync(List<Game> allGames, UserProfile userProfile)
        {
            var scoredGames = new List<(Game Game, double Score)>();

            foreach (var game in allGames)
            {
                var score = CalculateGameScore(game, userProfile);
                scoredGames.Add((game, score));
            }

            // FILTRAR Y ORDENAR POR COMPATIBILIDAD
            return scoredGames
                .Where(x => x.Score > 0.1) // Umbral mínimo
                .OrderByDescending(x => x.Score)
                .Take(12)
                .Select(x => x.Game)
                .ToList();
        }

        private double CalculateGameScore(Game game, UserProfile userProfile)
        {
            double score = 0.0;

            // 1. COMPATIBILIDAD CON GÉNEROS (60% del score)
            if (game.Genres?.Any() == true && userProfile.PreferredGenres.Any())
            {
                foreach (var genre in game.Genres)
                {
                    var genreName = genre.Name ?? "Unknown";
                    if (userProfile.PreferredGenres.ContainsKey(genreName))
                    {
                        // Más puntos por géneros más preferidos
                        var genreWeight = Math.Min(userProfile.PreferredGenres[genreName] * 0.2, 1.0);
                        score += genreWeight * 0.6;
                    }
                }
            }

            // 2. RATING DEL JUEGO (30% del score)
            score += (game.Rating / 5.0) * 0.3;

            // 3. RECIENTEZ (10% del score)
            if (DateTime.TryParse(game.Released, out var releaseDate))
            {
                var yearsAgo = DateTime.Now.Year - releaseDate.Year;
                var recencyScore = Math.Max(0, 1.0 - (yearsAgo * 0.15));
                score += recencyScore * 0.1;
            }

            return score;
        }

        public async Task<List<Game>> GetSimilarGamesAsync(int gameId)
        {
            try
            {
                var targetGame = await _rawgService.GetGameDetailsAsync(gameId);
                if (targetGame == null) return new List<Game>();

                var popularGames = await GetPopularGamesAsync(20);
                var similarGames = new List<(Game Game, double Similarity)>();

                foreach (var game in popularGames.Where(g => g.Id != gameId))
                {
                    var similarity = CalculateGameSimilarity(targetGame, game);
                    if (similarity > 0.3)
                    {
                        similarGames.Add((game, similarity));
                    }
                }

                return similarGames
                    .OrderByDescending(x => x.Similarity)
                    .Take(4)
                    .Select(x => x.Game)
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ML: Error en juegos similares: {ex.Message}");
                return new List<Game>();
            }
        }

        private double CalculateGameSimilarity(Game game1, Game game2)
        {
            double similarity = 0.0;

            // SIMILITUD POR GÉNEROS
            if (game1.Genres != null && game2.Genres != null)
            {
                var commonGenres = game1.Genres.Select(g => g.Id)
                    .Intersect(game2.Genres.Select(g => g.Id))
                    .Count();
                var totalGenres = game1.Genres.Select(g => g.Id)
                    .Union(game2.Genres.Select(g => g.Id))
                    .Count();
                
                if (totalGenres > 0)
                    similarity += (double)commonGenres / totalGenres * 0.7;
            }

            // SIMILITUD POR RATING
            similarity += (1 - Math.Abs(game1.Rating - game2.Rating) / 5.0) * 0.3;

            return similarity;
        }

        private async Task<List<Game>> GetPopularGamesAsync(int count)
        {
            try
            {
                var response = await _rawgService.GetGamesAsync("", "", "", 1);
                return response?.Results?.Take(count).ToList() ?? new List<Game>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ML: Error obteniendo juegos populares: {ex.Message}");
                return new List<Game>();
            }
        }

        public async Task TrackUserInteractionAsync(string userId, int gameId, string action)
        {
            try
            {
                Console.WriteLine($"📊 ML Tracking: {userId} -> {action} -> Game {gameId}");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ ML: Error en tracking: {ex.Message}");
            }
        }

        public async Task<int> GetUserLibraryCountAsync(string userId)
        {
            return await _context.BibliotecaUsuario
                .Where(b => b.UsuarioId == userId)
                .CountAsync();
        }

        public async Task<string> GetUserTopGenreAsync(string userId)
        {
            var userProfile = await AnalyzeUserProfileAsync(userId);
            return userProfile.TopGenre;
        }
    }
}