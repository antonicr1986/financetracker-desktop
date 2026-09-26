using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceTracker.Desktop.Domain;
using FinanceTracker.Desktop.Localization;
using FinanceTracker.Desktop.Models;
using FinanceTracker.Desktop.Services;

namespace FinanceTracker.Desktop.ViewModels;

/// <summary>
/// Una opcion del desplegable de categoria del presupuesto. Id null es "Todas
/// las categorias": el presupuesto cuenta todo el tipo, no "sin categoria".
/// </summary>
public record BudgetCategoryOption(int? Id, string Name)
{
    public override string ToString() => Name;
}

/// <summary>
/// Formulario de presupuesto, el mismo para crear (existing == null) y editar,
/// como en la web y Android. Mismo patron que TransactionEditorViewModel.
/// </summary>
public partial class BudgetEditorViewModel : ObservableObject, IDisposable
{
    private readonly IBudgetService budgets;
    private readonly IDialogService dialogs;
    private readonly Localizer localizer;
    private readonly List<CategoryDto> allCategories;
    private readonly BudgetDto? existing;
    private readonly MonthKey centre;

    /// <summary>Como escribir el error actual; ver LoginViewModel.</summary>
    private Func<string>? error;

    public BudgetEditorViewModel(
        IBudgetService budgets,
        IDialogService dialogs,
        IEnumerable<CategoryDto> categories,
        Localizer localizer,
        MonthKey month,
        BudgetDto? existing = null)
    {
        this.budgets = budgets;
        this.dialogs = dialogs;
        this.localizer = localizer;
        this.existing = existing;
        allCategories = categories.OrderBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
        localizer.Changed += OnLanguageChanged;

        // Al editar, los meses se centran en el del presupuesto; al crear, en
        // el mes que se estaba viendo en el panel.
        centre = existing is null ? month : new MonthKey(existing.Year, existing.Month);

        if (existing is null)
        {
            IsExpense = true; // lo normal es limitar un gasto
        }
        else
        {
            Name = existing.Name;
            AmountText = TransactionForm.FormatAmountForInput(existing.Amount);
            IsExpense = existing.Type == TransactionType.Expense;
        }

        RefreshMonths(centre);
        RefreshCategories(existing?.CategoryId);
    }

    public bool IsEdit => existing is not null;
    public string Title => localizer.T(IsEdit ? "budgets.editTitle" : "budgets.dialogTitle");

    [ObservableProperty] private string name = "";
    [ObservableProperty] private string amountText = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIncome))]
    private bool isExpense;

    public bool IsIncome
    {
        get => !IsExpense;
        set => IsExpense = !value;
    }

    public ObservableCollection<MonthOption> Months { get; } = [];
    [ObservableProperty] private MonthOption? selectedMonth;

    /// <summary>"Todas las categorías" y las del tipo elegido: la API rechaza las del otro.</summary>
    public ObservableCollection<BudgetCategoryOption> Categories { get; } = [];
    [ObservableProperty] private BudgetCategoryOption? selectedCategory;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string errorMessage = "";

    public bool HasError => ErrorMessage.Length > 0;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    private bool isBusy;

    public bool SessionHasExpired { get; private set; }

    /// <summary>Mes del presupuesto guardado, para que el panel lo muestre.</summary>
    public MonthKey? SavedMonth { get; private set; }

    /// <summary>La ventana lo escucha para cerrarse. true = se guardo o borro algo.</summary>
    public event EventHandler<bool>? CloseRequested;

    partial void OnIsExpenseChanged(bool value) => RefreshCategories(SelectedCategory?.Id);

    private void RefreshMonths(MonthKey selected)
    {
        Months.Clear();
        foreach (var key in Budgets.MonthsAround(centre))
            Months.Add(new MonthOption(key, key.Label(localizer.Culture)));
        SelectedMonth = Months.FirstOrDefault(m => m.Key == selected);
    }

    private void RefreshCategories(int? preferredId)
    {
        var type = IsExpense ? TransactionType.Expense : TransactionType.Income;
        Categories.Clear();
        Categories.Add(new BudgetCategoryOption(null, localizer.T("budgets.allCategories")));
        foreach (var category in allCategories.Where(c => c.Type == type))
            Categories.Add(new BudgetCategoryOption(category.Id, category.Name));

        // La elegida si sigue siendo de este tipo; si no, "Todas las categorías".
        SelectedCategory = Categories.FirstOrDefault(c => c.Id == preferredId) ?? Categories[0];
    }

    private bool CanAct() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanAct))]
    private async Task SaveAsync()
    {
        SetError(null);

        var problem = BudgetForm.Validate(Name, AmountText);
        if (problem is not null)
        {
            SetError(() => problem switch
            {
                BudgetFormProblem.MissingName => localizer.T("errors.writeBudgetName"),
                BudgetFormProblem.NameTooLong => localizer.T("errors.budgetNameTooLong", BudgetForm.MaxNameLength),
                BudgetFormProblem.InvalidAmount => localizer.T("errors.invalidAmount"),
                _ => localizer.T("errors.amountPositive"),
            });
            return;
        }

        if (SelectedMonth is null)
        {
            SetError(() => localizer.T("errors.chooseMonth"));
            return;
        }

        var month = SelectedMonth.Key;
        var input = new BudgetInput(
            Name.Trim(),
            TransactionForm.ParseAmount(AmountText)!.Value,
            month.Month,
            month.Year,
            IsExpense ? TransactionType.Expense : TransactionType.Income,
            SelectedCategory?.Id);

        await RunAsync(async () =>
        {
            if (existing is null) await budgets.CreateBudgetAsync(input);
            else await budgets.UpdateBudgetAsync(existing.Id, input);
            SavedMonth = month;
        }, deleting: false);
    }

    [RelayCommand(CanExecute = nameof(CanAct))]
    private async Task DeleteAsync()
    {
        if (existing is null) return;

        var confirmed = dialogs.Confirm(localizer.T("budgets.deleteConfirm"),
            localizer.T("dialog.deleteConfirmBody", existing.Name), localizer.T("dialog.deleteYes"));
        if (!confirmed) return;

        await RunAsync(() => budgets.DeleteBudgetAsync(existing.Id), deleting: true);
    }

    private async Task RunAsync(Func<Task> call, bool deleting)
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
            SetError(() => e.Code switch
            {
                ApiException.NetworkError => localizer.ApiError(e.Code),
                ApiException.NotFound => localizer.T("errors.budgetGone"),
                ApiException.CategoryNotFound or ApiException.CategoryTypeMismatch =>
                    localizer.T("errors.budgetCategoryInvalid"),
                _ => localizer.T(deleting ? "errors.deleteBudgetFailed" : "errors.saveBudgetFailed"),
            });
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SetError(Func<string>? text)
    {
        error = text;
        ErrorMessage = text?.Invoke() ?? "";
    }

    /// <summary>Al cambiar de idioma: titulo, nombres de meses, "Todas las categorías" y el error.</summary>
    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(Title));
        RefreshMonths(SelectedMonth?.Key ?? centre);
        RefreshCategories(SelectedCategory?.Id);
        SetError(error);
    }

    public void Dispose() => localizer.Changed -= OnLanguageChanged;
}
