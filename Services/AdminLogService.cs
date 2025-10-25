using Microsoft.EntityFrameworkCore;
using Proyecto_Gaming.Data;
using Proyecto_Gaming.Models;

namespace Proyecto_Gaming.Services
{
    public interface IAdminLogService
    {
        Task LogAsync(string action, string? targetUser = null, string? performedBy = null);
        Task<List<AdminLog>> GetRecentLogsAsync(int count = 10);
    }

    public class AdminLogService : IAdminLogService
    {
        private readonly ApplicationDbContext _db;
        public AdminLogService(ApplicationDbContext db) => _db = db;

        public async Task LogAsync(string action, string? targetUser = null, string? performedBy = null)
        {
            _db.AdminLogs.Add(new AdminLog
            {
                Action = action,
                TargetUser = targetUser,
                PerformedBy = performedBy,
                Timestamp = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }

        public async Task<List<AdminLog>> GetRecentLogsAsync(int count = 10) =>
            await _db.AdminLogs.OrderByDescending(x => x.Timestamp)
                               .Take(count)
                               .ToListAsync();
    }
}
