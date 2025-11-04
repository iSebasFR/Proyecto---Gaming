using Microsoft.EntityFrameworkCore;
using Proyecto_Gaming.Data;                 // ApplicationDbContext
using Proyecto_Gaming.Models.Surveys;       // Medal + UserMedal

namespace Proyecto_Gaming.Services
{
    public interface IRewardService
    {
        Task<List<Medal>> GetUserMedalsAsync(string userId);
        Task GrantAsync(string userId, string medalName); // <- buscamos por Name
    }

    public class RewardService : IRewardService
    {
        private readonly ApplicationDbContext _db;

        public RewardService(ApplicationDbContext db)
        {
            _db = db;
        }

        public Task<List<Medal>> GetUserMedalsAsync(string userId)
        {
            return _db.UserMedals
                .Where(um => um.UsuarioId == userId)
                .Include(um => um.Medal)
                .Select(um => um.Medal!)
                .OrderBy(m => m.Id)                 // usa Order si lo tienes
                .ToListAsync();
        }

        // Busca la medalla por Name (insensible a mayúsculas)
        public async Task GrantAsync(string userId, string medalName)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(medalName))
                return;

            // Match por Name, insensible a mayúsculas (EF-friendly)
            var medal = await _db.Medals
                .FirstOrDefaultAsync(m =>
                    m.Name != null &&
                    EF.Functions.Like(m.Name, medalName));

            if (medal == null) return;

            var exists = await _db.UserMedals
                .AnyAsync(um => um.UsuarioId == userId && um.MedalId == medal.Id);
            if (exists) return;

            _db.UserMedals.Add(new UserMedal
            {
                UsuarioId    = userId,              // <- tus nombres reales
                MedalId      = medal.Id,
                GrantedAtUtc = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
        }
    }
}
