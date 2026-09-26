using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceTracker.Desktop.Domain;
using FinanceTracker.Desktop.Localization;
using FinanceTracker.Desktop.Models;
using FinanceTracker.Desktop.Services;

namespace FinanceTracker.Desktop.ViewModels;

/// <summary>
/// Formulario de movimiento. El mismo para crear (existing == null) y para
/// editar, como en la web y en Android.
/// </summary>
public partial class TransactionEditorViewModel : ObservableObject, IDisposable
{
    private readonly Localizer localizer;

    /// <summary>Como escribir el error actual; ver LoginViewModel.</summary>
    private Func<string>? error;

    private readonly ITransactionService transactions;
    private readonly IDialogService dialogs;
    private readonly ICategoryService? categoryService;
    private readonly List<CategoryDto> allCategories;
    private readonly TransactionDto? existing;

    public TransactionEditorViewModel(
        ITransactionService transactions,
        IDialogService dialogs,
        IEnumerable<CategoryDto> categories,
        Localizer localizer,
        TransactionDto? existing = null,
        DateTime? today = null,
        ICategoryService? categoryService = null)
    {
        this.categoryService = categoryService;
        this.localizer = localizer;
        localizer.Changed += OnLanguageChanged;
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

    /// <summary>Sin servicio de categorias (algunos tests) no se ofrece crear.</summary>
    public bool CanCreateCategories => categoryService is not null;

    /// <summary>
    /// Categorias creadas desde este formulario. El panel las añade a su lista
    /// aunque el movimiento se cancele: la categoria ya existe en la API.
    /// </summary>
    public List<CategoryDto> CreatedCategories { get; } = [];

    /// <summary>Ids de las categorias borradas aqui: el panel las quita de su lista.</summary>
    public List<int> DeletedCategoryIds { get; } = [];

    /// <summary>Si se esta escribiendo una categoria nueva (caja y botones a la vista).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowNewCategoryLink))]
    private bool isAddingCategory;

    public bool ShowNewCategoryLink => CanCreateCategories && !IsAddingCategory;

    [ObservableProperty] private string newCategoryName = "";

    /// <summary>Aviso bajo el desplegable cuando no hay ninguna del tipo elegido.</summary>
    public bool HasNoCategoriesOfType => Categories.Count == 0;
    public string Title => localizer.T(IsEdit ? "dialog.editTitle" : "dialog.title");

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

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteCategoryCommand))]
    private CategoryDto? selectedCategory;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string errorMessage = "";

    public bool HasError => ErrorMessage.Length > 0;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(CreateCategoryCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCategoryCommand))]
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
        OnPropertyChanged(nameof(HasNoCategoriesOfType));
    }

    private bool CanAct() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanAct))]
    private async Task SaveAsync()
    {
        SetError(null);

        var problem = TransactionForm.Validate(Description, AmountText, Date, SelectedCategory is not null);
        if (problem is not null)
        {
            SetError(() => problem switch
            {
                TransactionFormProblem.MissingDescription => localizer.T("errors.writeConcept"),
                TransactionFormProblem.DescriptionTooLong =>
                    localizer.T("errors.conceptTooLong", TransactionForm.MaxDescriptionLength),
                TransactionFormProblem.InvalidAmount => localizer.T("errors.invalidAmount"),
                TransactionFormProblem.AmountNotPositive => localizer.T("errors.amountPositive"),
                TransactionFormProblem.MissingDate => localizer.T("errors.chooseDate"),
                _ => localizer.T("errors.chooseCategory"),
            });
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

        var confirmed = dialogs.Confirm(localizer.T("dialog.deleteConfirm"),
            localizer.T("dialog.deleteConfirmBody", existing.Description), localizer.T("dialog.deleteYes"));
        if (!confirmed) return;

        await RunAsync(() => transactions.DeleteTransactionAsync(existing.Id), deleting: true);
    }

    /// <summary>Ejecuta la llamada a la API y traduce los errores. Cierra si va bien.</summary>
    private async Task RunAsync(Func<Task> call, bool deleting = false)
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
            // Los codigos que conocemos tienen su frase; cualquier otro, la
            // generica de guardar o eliminar segun lo que se estaba haciendo.
            var fallback = deleting ? "errors.deleteFailed" : "errors.saveFailed";
            SetError(() => localizer.Texts.ContainsKey("apiError." + e.Code)
                ? localizer.ApiError(e.Code)
                : localizer.T(fallback));
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ===== Categoria nueva, como en la web: enlace, caja, Añadir / Cancelar =====

    [RelayCommand]
    private void StartAddingCategory()
    {
        NewCategoryName = "";
        IsAddingCategory = true;
    }

    [RelayCommand]
    private void CancelAddingCategory()
    {
        IsAddingCategory = false;
        NewCategoryName = "";
        SetError(null);
    }

    [RelayCommand(CanExecute = nameof(CanAct))]
    private async Task CreateCategoryAsync()
    {
        if (categoryService is null) return;
        SetError(null);

        var name = NewCategoryName.Trim();
        // La categoria hereda el tipo del movimiento: es el unico con el que la
        // API la aceptaria despues para este movimiento.
        var type = IsExpense ? TransactionType.Expense : TransactionType.Income;

        if (name.Length == 0)
        {
            SetError(() => localizer.T("errors.writeCategoryName"));
            return;
        }

        if (TransactionForm.IsDuplicateCategoryName(allCategories, name, type))
        {
            SetError(() => localizer.T("errors.categoryExists"));
            return;
        }

        IsBusy = true;
        try
        {
            var created = await categoryService.CreateCategoryAsync(new CategoryInput(name, type));
            CreatedCategories.Add(created);
            allCategories.Add(created);
            allCategories.Sort((a, b) => StringComparer.CurrentCultureIgnoreCase.Compare(a.Name, b.Name));

            // La recien creada queda elegida, como en la web y Android.
            RefreshCategories(preferredId: created.Id);
            IsAddingCategory = false;
            NewCategoryName = "";
        }
        catch (ApiException e) when (e.Code == ApiException.SessionExpired)
        {
            SessionHasExpired = true;
            CloseRequested?.Invoke(this, false);
        }
        catch (ApiException e)
        {
            SetError(() => e.Code == ApiException.NetworkError
                ? localizer.ApiError(e.Code)
                : localizer.T("errors.createCategoryFailed"));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanDeleteCategory() => CanCreateCategories && !IsBusy && SelectedCategory is not null;

    /// <summary>
    /// Borra la categoria elegida en el desplegable, tras confirmarlo. Solo la
    /// del propio usuario: la API filtra por el usuario del token. Si tiene
    /// movimientos, la API lo rechaza y se muestra su mensaje.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanDeleteCategory))]
    private async Task DeleteCategoryAsync()
    {
        if (categoryService is null || SelectedCategory is not { } category) return;
        SetError(null);

        var confirmed = dialogs.Confirm(localizer.T("dialog.deleteCategoryConfirm"),
            localizer.T("dialog.deleteConfirmBody", category.Name), localizer.T("dialog.deleteYes"));
        if (!confirmed) return;

        IsBusy = true;
        try
        {
            await categoryService.DeleteCategoryAsync(category.Id);
            Forget(category.Id);
        }
        catch (ApiException e) when (e.Code == ApiException.SessionExpired)
        {
            SessionHasExpired = true;
            CloseRequested?.Invoke(this, false);
        }
        catch (ApiException e) when (e.Code == ApiException.NotFound)
        {
            // Ya no estaba (borrada desde otro cliente): se quita igualmente.
            Forget(category.Id);
            SetError(() => localizer.T("errors.categoryGone"));
        }
        catch (ApiException e)
        {
            SetError(() => e.Code is ApiException.CategoryHasTransactions or ApiException.NetworkError
                ? localizer.ApiError(e.Code)
                : localizer.T("errors.deleteCategoryFailed"));
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Quita una categoria de este formulario y la apunta para el panel.</summary>
    private void Forget(int id)
    {
        allCategories.RemoveAll(c => c.Id == id);
        CreatedCategories.RemoveAll(c => c.Id == id);
        DeletedCategoryIds.Add(id);
        RefreshCategories(preferredId: null);
    }

    private void SetError(Func<string>? text)
    {
        error = text;
        ErrorMessage = text?.Invoke() ?? "";
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(Title));
        SetError(error);
    }

    /// <summary>Deja de escuchar al Localizer al cerrar el formulario.</summary>
    public void Dispose() => localizer.Changed -= OnLanguageChanged;
}
