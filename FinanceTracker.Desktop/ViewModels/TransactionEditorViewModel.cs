using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceTracker.Desktop.Domain;
using FinanceTracker.Desktop.Models;
using FinanceTracker.Desktop.Services;

namespace FinanceTracker.Desktop.ViewModels;

/// <summary>
/// Formulario de movimiento. El mismo para crear (existing == null) y para
/// editar, como en la web y en Android.
/// </summary>
public partial class TransactionEditorViewModel : ObservableObject
{
    private readonly ITransactionService transactions;
    private readonly IDialogService dialogs;
    private readonly List<CategoryDto> allCategories;
    private readonly TransactionDto? existing;

    public TransactionEditorViewModel(
        ITransactionService transactions,
        IDialogService dialogs,
        IEnumerable<CategoryDto> categories,
        TransactionDto? existing = null,
        DateTime? today = null)
    {
        this.transactions = transactions;
        this.dialogs = dialogs;
        this.existing = existing;
        allCategories = categories.OrderBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase).ToList();

        // Se usan las propiedades y no los campos: el toolkit avisa si se
        // escribe directamente en el campo generado.
        if (existing is null)
        {
            // Lo normal es apuntar un gasto de hoy.
            IsExpense = true;
            Date = (today ?? DateTime.Today).Date;
        }
        else
        {
            Description = existing.Description;
            AmountText = TransactionForm.FormatAmountForInput(existing.Amount);
            Date = existing.Date.Date;
            IsExpense = existing.Type == TransactionType.Expense;
        }

        RefreshCategories(preferredId: existing?.CategoryId);
    }

    public bool IsEdit => existing is not null;
    public string Title => IsEdit ? "Editar movimiento" : "Nuevo movimiento";

    [ObservableProperty] private string description = "";
    [ObservableProperty] private string amountText = "";
    [ObservableProperty] private DateTime? date;

    /// <summary>Gasto o ingreso. Dos RadioButton enlazados a esto y a IsIncome.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIncome))]
    private bool isExpense;

    public bool IsIncome
    {
        get => !IsExpense;
        set => IsExpense = !value;
    }

    /// <summary>Solo las categorias del tipo elegido: la API rechaza las del otro.</summary>
    public ObservableCollection<CategoryDto> Categories { get; } = [];

    [ObservableProperty] private CategoryDto? selectedCategory;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string errorMessage = "";

    public bool HasError => ErrorMessage.Length > 0;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    private bool isBusy;

    /// <summary>true si la API rechazo el token: el panel se encarga de volver al login.</summary>
    public bool SessionHasExpired { get; private set; }

    /// <summary>La ventana lo escucha para cerrarse. true = se guardo o borro algo.</summary>
    public event EventHandler<bool>? CloseRequested;

    /// <summary>Fecha del movimiento guardado, para que el panel muestre su mes.</summary>
    public DateTime? SavedDate { get; private set; }

    partial void OnIsExpenseChanged(bool value) => RefreshCategories(preferredId: SelectedCategory?.Id);

    private void RefreshCategories(int? preferredId)
    {
        var type = IsExpense ? TransactionType.Expense : TransactionType.Income;
        Categories.Clear();
        foreach (var category in allCategories.Where(c => c.Type == type))
            Categories.Add(category);

        // Se mantiene la elegida si es de este tipo; si no, la primera.
        SelectedCategory = Categories.FirstOrDefault(c => c.Id == preferredId) ?? Categories.FirstOrDefault();
    }

    private bool CanAct() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanAct))]
    private async Task SaveAsync()
    {
        ErrorMessage = "";

        var problem = TransactionForm.Validate(Description, AmountText, Date, SelectedCategory is not null);
        if (problem is not null)
        {
            ErrorMessage = problem switch
            {
                TransactionFormProblem.MissingDescription => "Escribe una descripción.",
                TransactionFormProblem.DescriptionTooLong =>
                    $"La descripción no puede pasar de {TransactionForm.MaxDescriptionLength} caracteres.",
                TransactionFormProblem.InvalidAmount => "El importe no es un número válido. Usa coma o punto para los decimales.",
                TransactionFormProblem.AmountNotPositive => "El importe tiene que ser mayor que cero.",
                TransactionFormProblem.MissingDate => "Elige una fecha.",
                _ => IsExpense
                    ? "No tienes categorías de gasto. Crea una desde la web o la app Android."
                    : "No tienes categorías de ingreso. Crea una desde la web o la app Android.",
            };
            return;
        }

        var input = new TransactionInput(
            Description.Trim(),
            TransactionForm.ParseAmount(AmountText)!.Value,
            // Solo el dia, sin hora ni zona: la API lo guarda tal cual.
            DateTime.SpecifyKind(Date!.Value.Date, DateTimeKind.Unspecified),
            IsExpense ? TransactionType.Expense : TransactionType.Income,
            SelectedCategory!.Id);

        await RunAsync(async () =>
        {
            if (existing is null) await transactions.CreateTransactionAsync(input);
            else await transactions.UpdateTransactionAsync(existing.Id, input);
            SavedDate = input.Date;
        });
    }

    [RelayCommand(CanExecute = nameof(CanAct))]
    private async Task DeleteAsync()
    {
        if (existing is null) return;

        var confirmed = dialogs.Confirm("Borrar movimiento",
            $"¿Seguro que quieres borrar \"{existing.Description}\"? No se puede deshacer.");
        if (!confirmed) return;

        await RunAsync(() => transactions.DeleteTransactionAsync(existing.Id));
    }

    /// <summary>Ejecuta la llamada a la API y traduce los errores. Cierra si va bien.</summary>
    private async Task RunAsync(Func<Task> call)
    {
        IsBusy = true;
        try
        {
            await call();
            CloseRequested?.Invoke(this, true);
        }
        catch (ApiException e) when (e.Code == ApiException.SessionExpired)
        {
            SessionHasExpired = true;
            CloseRequested?.Invoke(this, false);
        }
        catch (ApiException e)
        {
            ErrorMessage = e.Code switch
            {
                ApiException.NetworkError => "No se pudo conectar con el servidor. Revisa tu conexión.",
                ApiException.NotFound => "Este movimiento ya no existe. Cierra y recarga.",
                ApiException.CategoryNotFound => "La categoría ya no existe. Elige otra.",
                ApiException.CategoryTypeMismatch => "La categoría no es del tipo elegido.",
                _ => "No se pudo guardar. Inténtalo de nuevo.",
            };
        }
        finally
        {
            IsBusy = false;
        }
    }
}
