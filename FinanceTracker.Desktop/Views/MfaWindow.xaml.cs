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
        Closed += (_, _) => viewModel.Dispose();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
