using FinanceTracker.Desktop.ViewModels;

namespace FinanceTracker.Desktop.Services;

/// <summary>
/// Los ViewModels no pueden abrir ventanas ni MessageBox: no se podrian probar.
/// Piden los dialogos a traves de esta interfaz; en la app la implementa
/// DialogService con ventanas de verdad, y en los tests un falso que responde
/// lo que le digamos.
/// </summary>
public interface IDialogService
{
    /// <summary>Abre el formulario de movimiento. true si se guardo o se borro algo.</summary>
    bool ShowTransactionEditor(TransactionEditorViewModel editor);

    /// <summary>
    /// Pregunta si/no. true si el usuario confirma. confirmText es el texto del
    /// boton que confirma ("Sí, eliminar"); el otro siempre es "Cancelar".
    /// </summary>
    bool Confirm(string title, string message, string confirmText);
}
