using Proyecto_Gaming.Models;

namespace Proyecto_Gaming.Services
{
    public interface ILogService
    {
        Task LogAsync(string action, string? targetUser = null, string? performedBy = null);
        Task<List<AdminLog>> GetRecentLogsAsync(int limit = 15);
    }
}
