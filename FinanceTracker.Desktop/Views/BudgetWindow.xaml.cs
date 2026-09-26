using System.Windows;
using FinanceTracker.Desktop.ViewModels;

namespace FinanceTracker.Desktop.Views;

public partial class BudgetWindow : Window
{
    public BudgetWindow(BudgetEditorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // El ViewModel pide cerrar; ShowDialog() devuelve lo que se ponga aqui.
        viewModel.CloseRequested += (_, changed) => DialogResult = changed;

        Loaded += (_, _) => NameInput.Focus();
    }

    // La X de la barra propia: cierra sin guardar, igual que Cancelar o Escape.
    private void OnCloseClick(object sender, RoutedEventArgs e) => DialogResult = false;
}
