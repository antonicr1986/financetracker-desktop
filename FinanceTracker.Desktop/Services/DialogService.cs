using System.Windows;
using FinanceTracker.Desktop.ViewModels;
using FinanceTracker.Desktop.Views;

namespace FinanceTracker.Desktop.Services;

/// <summary>Implementacion real de IDialogService, con ventanas de WPF.</summary>
public class DialogService : IDialogService
{
    public bool ShowTransactionEditor(TransactionEditorViewModel editor)
    {
        var window = new TransactionWindow(editor) { Owner = ActiveWindow() };
        return window.ShowDialog() == true;
    }

    // Ventana propia y no MessageBox: sigue el tema y el idioma de la aplicacion.
    public bool Confirm(string title, string message, string confirmText)
    {
        var window = new ConfirmWindow(title, message, confirmText) { Owner = ActiveWindow() };
        return window.ShowDialog() == true;
    }

    /// <summary>La ventana que esta delante, para que el dialogo salga centrado sobre ella.</summary>
    private static Window? ActiveWindow() =>
        Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
        ?? Application.Current.MainWindow;
}
