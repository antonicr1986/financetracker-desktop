using System.Reflection;
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

        // Al pasar al codigo del MFA, el cursor va directo a su caja.
        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(LoginViewModel.IsMfaStep) && viewModel.IsMfaStep)
                Dispatcher.BeginInvoke(new Action(() => MfaCodeInput.Focus()));
            if (e.PropertyName == nameof(LoginViewModel.IsEmailStep) && viewModel.IsEmailStep)
                Dispatcher.BeginInvoke(new Action(() => EmailCodeInput.Focus()));
        };
        VersionText.Text = $"v{AppVersion()}";
    }

    // La version sale de la etiqueta del release (-p:Version en el pipeline).
    // .NET le anade "+<commit>" al final; solo se muestra la parte de antes.
    private static string AppVersion()
    {
        var version = Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "?";
        return version.Split('+')[0];
    }

    // Excepcion a "nada de logica en el code-behind": WPF no deja
    // hacer binding de PasswordBox.Password, asi que se pasa a mano.
    private void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        viewModel.Password = PasswordInput.Password;
    }
}
