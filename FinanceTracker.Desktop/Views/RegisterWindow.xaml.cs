using System.Windows;
using FinanceTracker.Desktop.ViewModels;

namespace FinanceTracker.Desktop.Views;

public partial class RegisterWindow : Window
{
    private readonly RegisterViewModel viewModel;

    public RegisterWindow(RegisterViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        DataContext = viewModel;

        Loaded += (_, _) => NameInput.Focus();
    }

    // Como en el login: WPF no deja hacer binding de PasswordBox.Password.
    private void OnPasswordChanged(object sender, RoutedEventArgs e) => viewModel.Password = PasswordInput.Password;

    private void OnConfirmationChanged(object sender, RoutedEventArgs e) =>
        viewModel.Confirmation = ConfirmationInput.Password;
}
