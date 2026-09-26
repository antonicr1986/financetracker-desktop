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

    /// <summary>Crea una categoria y devuelve la creada (201), con su id.</summary>
    Task<CategoryDto> CreateCategoryAsync(CategoryInput input);

    /// <summary>
    /// Borra una categoria del usuario. La API filtra por el usuario del token:
    /// una categoria de otra cuenta responde 404 como si no existiera. Con
    /// movimientos, rechaza con category_has_transactions.
    /// </summary>
    Task DeleteCategoryAsync(int id);
}

public interface IBudgetService
{
    /// <summary>Todos los presupuestos de todos los meses: la API no admite filtro.</summary>
    Task<List<BudgetDto>> GetBudgetsAsync();

    Task<BudgetDto> CreateBudgetAsync(BudgetInput input);

    /// <summary>204 sin cuerpo, como el resto de PUT.</summary>
    Task UpdateBudgetAsync(int id, BudgetInput input);

    Task DeleteBudgetAsync(int id);
}
