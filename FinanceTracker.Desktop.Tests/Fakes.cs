using FinanceTracker.Desktop.Models;
using FinanceTracker.Desktop.Services;
using FinanceTracker.Desktop.ViewModels;

namespace FinanceTracker.Desktop.Tests;

/// <summary>API falsa: devuelve lo que le digamos y apunta lo que se le pide.</summary>
internal class FakeTransactionService : ITransactionService, ICategoryService
{
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

    public bool ShowTransactionEditor(TransactionEditorViewModel editor)
    {
        LastEditor = editor;
        return Editor(editor);
    }

    public bool Confirm(string title, string message)
    {
        ConfirmCount++;
        return ConfirmAnswer;
    }
}
