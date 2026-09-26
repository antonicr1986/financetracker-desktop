using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceTracker.Desktop.Domain;
using FinanceTracker.Desktop.Models;
using FinanceTracker.Desktop.Services;

namespace FinanceTracker.Desktop.ViewModels;

public record MonthOption(MonthKey Key, string Label);

/// <summary>
/// Panel principal: selector de mes, totales del mes y lista de movimientos.
/// Se descarga todo una vez y el cambio de mes se resuelve en memoria.
/// </summary>
public partial class MainViewModel(Session session, ITransactionService transactions) : ObservableObject
{
    // Siempre en euros y con formato español, como la web y Android. El
    // cambio de idioma llegara en un paso posterior.
    public static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("es-ES");

    private List<TransactionDto> all = [];

    public string Greeting => $"Hola, {session.User?.Name}";
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
        ErrorMessage = "";
        IsLoading = true;
        try
        {
            all = await transactions.GetAllTransactionsAsync();
            Render();
        }
        catch (ApiException e) when (e.Code == ApiException.SessionExpired)
        {
            session.Clear();
            SessionExpired?.Invoke(this, EventArgs.Empty);
        }
        catch (ApiException e)
        {
            ErrorMessage = e.Code == ApiException.NetworkError
                ? "No se pudo conectar con el servidor. Revisa tu conexión."
                : "No se pudieron cargar los movimientos.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void Render()
    {
        var previous = SelectedMonth?.Key;

        Months.Clear();
        foreach (var key in Domain.Months.Available(all))
            Months.Add(new MonthOption(key, key.Label(Culture)));

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
            Transactions.Add(TransactionRow.From(t, Culture));

        var summary = Domain.Months.Summary(ofMonth);
        IncomeText = summary.Income.ToString("C", Culture);
        ExpenseText = summary.Expense.ToString("C", Culture);
        BalanceText = summary.Balance.ToString("C", Culture);
        IsBalanceNegative = summary.Balance < 0;
    }

    [RelayCommand]
    private void Logout()
    {
        session.Clear();
        LoggedOut?.Invoke(this, EventArgs.Empty);
    }
}
