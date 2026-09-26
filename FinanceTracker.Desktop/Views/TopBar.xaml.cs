using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FinanceTracker.Desktop.Views;

public partial class TopBar : UserControl
{
    /// <summary>
    /// Comando de "Salir". Sin comando (en el login) el boton queda
    /// deshabilitado, igual que en la web y Android.
    /// </summary>
    public static readonly DependencyProperty SignOutCommandProperty = DependencyProperty.Register(
        nameof(SignOutCommand), typeof(ICommand), typeof(TopBar),
        new PropertyMetadata(null, (d, e) => ((TopBar)d).UpdateSignOut()));

    public ICommand? SignOutCommand
    {
        get => (ICommand?)GetValue(SignOutCommandProperty);
        set => SetValue(SignOutCommandProperty, value);
    }

    public TopBar()
    {
        InitializeComponent();
        UpdateSignOut();
    }

    private void UpdateSignOut()
    {
        SignOutButton.Command = SignOutCommand;
        SignOutButton.IsEnabled = SignOutCommand is not null;
    }

    // El tema es cosa de la interfaz, no de ninguna pantalla en concreto: el
    // boton habla directamente con el ThemeManager de la aplicacion.
    private void OnThemeClick(object sender, RoutedEventArgs e) => App.Theme.Toggle();
}
