using System.Windows;
using FinanceTracker.Desktop.ViewModels;

namespace FinanceTracker.Desktop.Views;

public partial class MfaWindow : Window
{
    public MfaWindow(MfaViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Si la sesion caduca mientras esta abierta, se cierra; el panel
        // (que escucha el mismo evento) vuelve al login.
        viewModel.SessionExpired += (_, _) => Close();
        viewModel.SignedOut += (_, _) => Close();
        viewModel.PasswordFormCleared += (_, _) =>
        {
            CurrentPasswordInput.Clear();
            NewPasswordInput.Clear();
            ConfirmPasswordInput.Clear();
        };
        Closed += (_, _) => viewModel.Dispose();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    // WPF no deja hacer binding de PasswordBox.Password: se copia a mano.
    private void OnCurrentPasswordChanged(object sender, RoutedEventArgs e) =>
        ((MfaViewModel)DataContext).CurrentPassword = CurrentPasswordInput.Password;

    private void OnNewPasswordChanged(object sender, RoutedEventArgs e) =>
        ((MfaViewModel)DataContext).NewPassword = NewPasswordInput.Password;

    private void OnConfirmPasswordChanged(object sender, RoutedEventArgs e) =>
        ((MfaViewModel)DataContext).PasswordConfirmation = ConfirmPasswordInput.Password;
}
