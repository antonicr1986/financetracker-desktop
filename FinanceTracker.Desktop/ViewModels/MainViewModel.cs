using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceTracker.Desktop.Services;

namespace FinanceTracker.Desktop.ViewModels;

/// <summary>
/// Pantalla principal. De momento solo saluda y permite salir; en el siguiente
/// paso cargara los movimientos.
/// </summary>
public partial class MainViewModel(Session session) : ObservableObject
{
    public string Greeting => $"Hola, {session.User?.Name}";

    public string Email => session.User?.Email ?? "";

    public event EventHandler? LoggedOut;

    [RelayCommand]
    private void Logout()
    {
        session.Clear();
        LoggedOut?.Invoke(this, EventArgs.Empty);
    }
}
