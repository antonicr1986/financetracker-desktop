using System.Windows;
using FinanceTracker.Desktop.ViewModels;

namespace FinanceTracker.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Lo unico que hace el code-behind: decirle a la ventana cual es su
        // ViewModel. Todos los {Binding} del XAML se resuelven contra el.
        DataContext = new MainViewModel();
    }
}
