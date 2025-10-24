using System.Threading.Tasks;
using Proyecto_Gaming.Models.Payment;

namespace Proyecto_Gaming.Services
{
    public interface IPaymentService
    {
        Task<string> CreateCheckoutSessionAsync(int gameId, string userId, decimal amount, string gameTitle, string successUrl, string cancelUrl);
        Task<bool> ConfirmPaymentAsync(string sessionId);
        Task<Transaction> GetTransactionBySessionIdAsync(string sessionId);
    }
}