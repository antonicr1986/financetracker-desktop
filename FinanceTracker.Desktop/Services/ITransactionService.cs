using FinanceTracker.Desktop.Models;

namespace FinanceTracker.Desktop.Services;

public interface ITransactionService
{
    /// <summary>Todos los movimientos del usuario, recorriendo todas las paginas.</summary>
    Task<List<TransactionDto>> GetAllTransactionsAsync();
}
