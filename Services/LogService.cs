using Microsoft.EntityFrameworkCore;
using Proyecto_Gaming.Data;
using Proyecto_Gaming.Models;

namespace Proyecto_Gaming.Services
{
    public class LogService : ILogService
    {
        private readonly ApplicationDbContext _db;

        public LogService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task LogAsync(string action, string? targetUser = null, string? performedBy = null)
        {
            var log = new AdminLog
            {
                Action = action,
                TargetUser = targetUser,
                PerformedBy = performedBy,
                Timestamp = DateTime.UtcNow
            };
            _db.AdminLogs.Add(log);
            await _db.SaveChangesAsync();
        }

        public async Task<List<AdminLog>> GetRecentLogsAsync(int limit = 15)
        {
            return await _db.AdminLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(limit)
                .ToListAsync();
        }
    }
}
