using System.Windows;
using FinanceTracker.Desktop.ViewModels;

namespace FinanceTracker.Desktop.Views;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel viewModel;

    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        DataContext = viewModel;

        Loaded += (_, _) => EmailInput.Focus();
    }

    // La unica excepcion a "nada de logica en el code-behind": WPF no deja
    // hacer binding de PasswordBox.Password, asi que se pasa a mano.
    private void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        viewModel.Password = PasswordInput.Password;
    }
}
