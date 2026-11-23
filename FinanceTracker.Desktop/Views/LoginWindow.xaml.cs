using System.Windows;
using FinanceTracker.Desktop.Services;
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

        // Al pasar al codigo del MFA, el cursor va directo a su caja.
        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(LoginViewModel.IsMfaStep) && viewModel.IsMfaStep)
                Dispatcher.BeginInvoke(new Action(() => MfaCodeInput.Focus()));
            if (e.PropertyName == nameof(LoginViewModel.IsEmailStep) && viewModel.IsEmailStep)
                Dispatcher.BeginInvoke(new Action(() => EmailCodeInput.Focus()));
        };
        VersionText.Text = $"v{AppInfo.Version}";
    }

    // Excepcion a "nada de logica en el code-behind": WPF no deja
    // hacer binding de PasswordBox.Password, asi que se pasa a mano.
    private void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        viewModel.Password = PasswordInput.Password;
    }
}
