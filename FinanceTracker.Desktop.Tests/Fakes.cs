using FinanceTracker.Desktop.Models;
using FinanceTracker.Desktop.Services;
using FinanceTracker.Desktop.ViewModels;

namespace FinanceTracker.Desktop.Tests;

/// <summary>API falsa: devuelve lo que le digamos y apunta lo que se le pide.</summary>
internal class FakeTransactionService : ITransactionService, ICategoryService, IBudgetService
{
    public List<BudgetDto> Budgets { get; set; } = [];
    public Task<List<BudgetDto>> GetBudgetsAsync() => Task.FromResult(Budgets);

    public List<BudgetInput> CreatedBudgets { get; } = [];
    public List<(int Id, BudgetInput Input)> UpdatedBudgets { get; } = [];
    public List<int> DeletedBudgets { get; } = [];

    /// <summary>Si se asigna, las escrituras de presupuestos lanzan esta excepcion.</summary>
    public ApiException? BudgetError { get; set; }

    public Task<BudgetDto> CreateBudgetAsync(BudgetInput input)
    {
        if (BudgetError is not null) throw BudgetError;
        CreatedBudgets.Add(input);
        return Task.FromResult(new BudgetDto(500, input.Name, input.Amount, 0, input.Amount, 0,
            input.Month, input.Year, input.Type, input.CategoryId, null));
    }

    public Task UpdateBudgetAsync(int id, BudgetInput input)
    {
        if (BudgetError is not null) throw BudgetError;
        UpdatedBudgets.Add((id, input));
        return Task.CompletedTask;
    }

    public Task DeleteBudgetAsync(int id)
    {
        if (BudgetError is not null) throw BudgetError;
        DeletedBudgets.Add(id);
        return Task.CompletedTask;
    }

    public Func<List<TransactionDto>> Handler { get; set; } = () => [];
    public List<CategoryDto> Categories { get; set; } = [];

    /// <summary>Si se asigna, las escrituras lanzan esta excepcion.</summary>
    public ApiException? WriteError { get; set; }

    public List<TransactionInput> Created { get; } = [];
    public List<(int Id, TransactionInput Input)> Updated { get; } = [];
    public List<int> Deleted { get; } = [];
    public int LoadCount { get; private set; }

    public Task<List<TransactionDto>> GetAllTransactionsAsync()
    {
        LoadCount++;
        return Task.FromResult(Handler());
    }

    public Task<List<CategoryDto>> GetCategoriesAsync() => Task.FromResult(Categories);

    public List<CategoryInput> CreatedCategories { get; } = [];

    /// <summary>Si se asigna, crear categoria lanza esta excepcion.</summary>
    public ApiException? CategoryError { get; set; }

    public List<int> DeletedCategories { get; } = [];

    /// <summary>Si se asigna, borrar categoria lanza esta excepcion.</summary>
    public ApiException? DeleteCategoryError { get; set; }

    public Task DeleteCategoryAsync(int id)
    {
        if (DeleteCategoryError is not null) throw DeleteCategoryError;
        DeletedCategories.Add(id);
        return Task.CompletedTask;
    }

    public Task<CategoryDto> CreateCategoryAsync(CategoryInput input)
    {
        if (CategoryError is not null) throw CategoryError;
        CreatedCategories.Add(input);
        return Task.FromResult(new CategoryDto(100 + CreatedCategories.Count, input.Name, input.Type));
    }

    public Task<TransactionDto> CreateTransactionAsync(TransactionInput input)
    {
        if (WriteError is not null) throw WriteError;
        Created.Add(input);
        return Task.FromResult(new TransactionDto(99, input.Description, input.Amount, input.Date, input.Type,
            input.CategoryId, ""));
    }

    public Task UpdateTransactionAsync(int id, TransactionInput input)
    {
        if (WriteError is not null) throw WriteError;
        Updated.Add((id, input));
        return Task.CompletedTask;
    }

    public Task DeleteTransactionAsync(int id)
    {
        if (WriteError is not null) throw WriteError;
        Deleted.Add(id);
        return Task.CompletedTask;
    }
}

/// <summary>Dialogos falsos: no abren ventanas, responden lo configurado.</summary>
internal class FakeDialogService : IDialogService
{
    public bool ConfirmAnswer { get; set; } = true;
    public int ConfirmCount { get; private set; }

    /// <summary>Que hace el "usuario" en el formulario. Por defecto, cancelar.</summary>
    public Func<TransactionEditorViewModel, bool> Editor { get; set; } = _ => false;

    public TransactionEditorViewModel? LastEditor { get; private set; }

    /// <summary>Que hace el "usuario" en el formulario de presupuesto. Por defecto, cancelar.</summary>
    public Func<BudgetEditorViewModel, bool> BudgetEditor { get; set; } = _ => false;

    public BudgetEditorViewModel? LastBudgetEditor { get; private set; }

    public bool ShowBudgetEditor(BudgetEditorViewModel editor)
    {
        LastBudgetEditor = editor;
        return BudgetEditor(editor);
    }

    public bool ShowTransactionEditor(TransactionEditorViewModel editor)
    {
        LastEditor = editor;
        return Editor(editor);
    }

    public string? LastConfirmText { get; private set; }

    public bool Confirm(string title, string message, string confirmText)
    {
        ConfirmCount++;
        LastConfirmText = confirmText;
        return ConfirmAnswer;
    }
}
