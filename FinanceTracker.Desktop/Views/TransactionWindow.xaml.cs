using System.Windows;
using FinanceTracker.Desktop.ViewModels;

namespace FinanceTracker.Desktop.Views;

public partial class TransactionWindow : Window
{
    public TransactionWindow(TransactionEditorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // El ViewModel pide cerrar; la ventana decide como. DialogResult es lo
        // que devuelve ShowDialog() a quien la abrio.
        viewModel.CloseRequested += (_, changed) => DialogResult = changed;

        Loaded += (_, _) => DescriptionInput.Focus();

        // Al pulsar "Nueva categoría", el cursor va directo a su caja. Es cosa
        // de la vista (el foco), por eso vive aqui y no en el ViewModel.
        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(TransactionEditorViewModel.IsAddingCategory) && viewModel.IsAddingCategory)
                Dispatcher.BeginInvoke(() => NewCategoryInput.Focus());
        };
    }
}
