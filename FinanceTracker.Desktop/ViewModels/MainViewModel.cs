using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceTracker.Desktop.Domain;
using FinanceTracker.Desktop.Localization;
using FinanceTracker.Desktop.Models;
using FinanceTracker.Desktop.Services;

namespace FinanceTracker.Desktop.ViewModels;

public record MonthOption(MonthKey Key, string Label)
{
    // La parte cerrada de un ComboBox pinta el elemento elegido con ToString()
    // cuando la plantilla no le dice otra cosa. En un record eso seria
    // "MonthOption { Key = ..., Label = ... }": aqui devuelve solo el texto.
    public override string ToString() => Label;
}

/// <summary>
/// Panel principal: selector de mes, totales del mes y lista de movimientos.
/// Se descarga todo una vez y el cambio de mes se resuelve en memoria.
/// </summary>
public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly Session session;
    private readonly ITransactionService transactions;
    private readonly ICategoryService categories;
    private readonly IDialogService dialogs;
    private readonly Localizer localizer;

    /// <summary>Como escribir el error actual; ver LoginViewModel.</summary>
    private Func<string>? error;

    public MainViewModel(
        Session session,
        ITransactionService transactions,
        ICategoryService categories,
        IDialogService dialogs,
        Localizer localizer)
    {
        this.session = session;
        this.transactions = transactions;
        this.categories = categories;
        this.dialogs = dialogs;
        this.localizer = localizer;
        localizer.Changed += OnLanguageChanged;
    }

    private List<TransactionDto> all = [];
    private List<CategoryDto> allCategories = [];

    /// <summary>Mes a mostrar tras la proxima carga (el del movimiento recien guardado).</summary>
    private MonthKey? monthAfterLoad;

    public string Greeting => localizer.T("dashboard.greeting", session.User?.Name ?? "");
    public string Email => session.User?.Email ?? "";

    public ObservableCollection<MonthOption> Months { get; } = [];
    public ObservableCollection<TransactionRow> Transactions { get; } = [];

    [ObservableProperty]
    private MonthOption? selectedMonth;

    [ObservableProperty] private string incomeText = "";
    [ObservableProperty] private string expenseText = "";
    [ObservableProperty] private string balanceText = "";
    [ObservableProperty] private bool isBalanceNegative;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasData))]
    [NotifyCanExecuteChangedFor(nameof(LoadCommand))]
    [NotifyCanExecuteChangedFor(nameof(NewTransactionCommand))]
    private bool isLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    [NotifyPropertyChangedFor(nameof(HasData))]
    private string errorMessage = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasData))]
    private bool isEmpty;

    public bool HasError => ErrorMessage.Length > 0;
    public bool HasData => !IsLoading && !HasError && !IsEmpty;

    public event EventHandler? LoggedOut;
    public event EventHandler? SessionExpired;

    private bool CanLoad() => !IsLoading;

    [RelayCommand(CanExecute = nameof(CanLoad))]
    private async Task LoadAsync()
    {
        SetError(null);
        IsLoading = true;
        try
        {
            // Las dos peticiones a la vez: no dependen una de otra.
            var transactionsTask = transactions.GetAllTransactionsAsync();
            var categoriesTask = categories.GetCategoriesAsync();
            await Task.WhenAll(transactionsTask, categoriesTask);

            all = transactionsTask.Result;
            allCategories = categoriesTask.Result;
            Render();
        }
        catch (ApiException e) when (e.Code == ApiException.SessionExpired)
        {
            session.Clear();
            SessionExpired?.Invoke(this, EventArgs.Empty);
        }
        catch (ApiException e)
        {
            SetError(() => e.Code == ApiException.NetworkError
                ? localizer.ApiError(e.Code)
                : localizer.T("errors.loadFailed"));
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void Render()
    {
        var previous = monthAfterLoad ?? SelectedMonth?.Key;
        monthAfterLoad = null;

        Months.Clear();
        foreach (var key in Domain.Months.Available(all))
            Months.Add(new MonthOption(key, key.Label(localizer.Culture)));

        IsEmpty = Months.Count == 0;

        // Tras recargar se mantiene el mes que se estaba viendo; si ya no
        // existe, o es la primera carga, el mas reciente.
        SelectedMonth = Months.FirstOrDefault(m => m.Key == previous) ?? Months.FirstOrDefault();
        ShowMonth();
    }

    partial void OnSelectedMonthChanged(MonthOption? value) => ShowMonth();

    private void ShowMonth()
    {
        Transactions.Clear();
        List<TransactionDto> ofMonth = SelectedMonth is null ? [] : Domain.Months.Of(all, SelectedMonth.Key);

        foreach (var t in ofMonth)
            Transactions.Add(TransactionRow.From(t, localizer.Culture));

        var summary = Domain.Months.Summary(ofMonth);
        IncomeText = summary.Income.ToString("C", localizer.Culture);
        ExpenseText = summary.Expense.ToString("C", localizer.Culture);
        BalanceText = summary.Balance.ToString("C", localizer.Culture);
        IsBalanceNegative = summary.Balance < 0;
    }

    [RelayCommand(CanExecute = nameof(CanLoad))]
    private Task NewTransactionAsync() =>
        OpenEditorAsync(new TransactionEditorViewModel(transactions, dialogs, allCategories, localizer));

    /// <summary>Doble clic en un movimiento: el mismo formulario, relleno.</summary>
    [RelayCommand]
    private Task EditTransactionAsync(TransactionRow? row) =>
        row is null
            ? Task.CompletedTask
            : OpenEditorAsync(new TransactionEditorViewModel(transactions, dialogs, allCategories, localizer, row.Source));

    private async Task OpenEditorAsync(TransactionEditorViewModel editor)
    {
        bool changed;
        using (editor)
            changed = dialogs.ShowTransactionEditor(editor);

        if (editor.SessionHasExpired)
        {
            session.Clear();
            SessionExpired?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (!changed) return;

        // Tras guardar se muestra el mes del movimiento, aunque sea otro.
        if (editor.SavedDate is { } savedDate) monthAfterLoad = MonthKey.Of(savedDate);

        // Se recarga todo: es lo mas simple y garantiza ver lo mismo que la API.
        await LoadAsync();
    }

    [RelayCommand]
    private void Logout()
    {
        session.Clear();
        LoggedOut?.Invoke(this, EventArgs.Empty);
    }

    private void SetError(Func<string>? text)
    {
        error = text;
        ErrorMessage = text?.Invoke() ?? "";
    }

    /// <summary>
    /// Al cambiar de idioma se rehace lo que lleva texto o formato: saludo,
    /// nombres de meses, importes, fechas y el error si lo hay. Los datos ya
    /// estan en memoria: no se vuelve a llamar a la API.
    /// </summary>
    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(Greeting));
        SetError(error);
        if (all.Count > 0) Render();
    }

    /// <summary>Deja de escuchar al Localizer, que vive mas que esta pantalla.</summary>
    public void Dispose() => localizer.Changed -= OnLanguageChanged;
}
