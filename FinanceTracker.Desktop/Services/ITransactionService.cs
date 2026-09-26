using FinanceTracker.Desktop.Models;

namespace FinanceTracker.Desktop.Services;

public interface ITransactionService
{
    /// <summary>Todos los movimientos del usuario, recorriendo todas las paginas.</summary>
    Task<List<TransactionDto>> GetAllTransactionsAsync();

    Task<TransactionDto> CreateTransactionAsync(TransactionInput input);

    Task UpdateTransactionAsync(int id, TransactionInput input);

    Task DeleteTransactionAsync(int id);
}

public interface ICategoryService
{
    Task<List<CategoryDto>> GetCategoriesAsync();
}

public interface IBudgetService
{
    /// <summary>Todos los presupuestos de todos los meses: la API no admite filtro.</summary>
    Task<List<BudgetDto>> GetBudgetsAsync();
}
