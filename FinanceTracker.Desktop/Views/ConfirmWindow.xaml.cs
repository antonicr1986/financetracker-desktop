using System.Windows;

namespace FinanceTracker.Desktop.Views;

/// <summary>
/// Dialogo de si/no con el estilo de la aplicacion. Es solo vista (titulo,
/// mensaje y dos botones), asi que no necesita ViewModel: quien lo abre es
/// DialogService, y los ViewModels lo piden a traves de IDialogService.
/// </summary>
public partial class ConfirmWindow : Window
{
    public ConfirmWindow(string title, string message, string confirmText)
    {
        InitializeComponent();
        Title = title;
        TitleText.Text = title;
        MessageText.Text = message;
        ConfirmButton.Content = confirmText;

        Loaded += (_, _) => CancelButton.Focus();
    }

    private void OnConfirm(object sender, RoutedEventArgs e) => DialogResult = true;
}
