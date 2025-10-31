using System.Threading.Tasks;

namespace Proyecto_Gaming.Services
{
    public interface IAuditService
    {
        Task RegisterActionAsync(string actor, string action, string targetId);
    }

    public class AuditService : IAuditService
    {
        public Task RegisterActionAsync(string actor, string action, string targetId)
        {
            Console.WriteLine($"[AUDIT] {actor} realizó {action} sobre {targetId}");
            return Task.CompletedTask;
        }
    }
}
