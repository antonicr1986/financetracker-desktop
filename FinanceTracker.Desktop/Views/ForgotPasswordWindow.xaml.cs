using System.Windows;
using FinanceTracker.Desktop.ViewModels;

namespace FinanceTracker.Desktop.Views;

public partial class ForgotPasswordWindow : Window
{
    private readonly ForgotPasswordViewModel viewModel;

    public ForgotPasswordWindow(ForgotPasswordViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        DataContext = viewModel;

        Loaded += (_, _) => EmailInput.Focus();

        // Al pasar al codigo, el cursor va a su caja; al volver, se vacian las contraseñas.
        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(ForgotPasswordViewModel.Step)) return;

            if (viewModel.IsCodeStep)
            {
                Dispatcher.BeginInvoke(new Action(() => CodeInput.Focus()));
            }
            else if (viewModel.IsEmailStep)
            {
                PasswordInput.Clear();
                ConfirmationInput.Clear();
                Dispatcher.BeginInvoke(new Action(() => EmailInput.Focus()));
            }
        };
    }

    // Como en el registro: WPF no deja hacer binding de PasswordBox.Password.
    private void OnPasswordChanged(object sender, RoutedEventArgs e) => viewModel.Password = PasswordInput.Password;

    private void OnConfirmationChanged(object sender, RoutedEventArgs e) =>
        viewModel.Confirmation = ConfirmationInput.Password;
}
