using System.Windows;
using FinanceTracker.Desktop.ViewModels;

namespace FinanceTracker.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Al volver a la aplicacion: si la sesion se cerro desde otro
        // dispositivo, se nota aqui y no al pulsar "Recargar".
        Activated += async (_, _) => await viewModel.RefreshOnReturnAsync(DateTime.UtcNow);
    }
}
