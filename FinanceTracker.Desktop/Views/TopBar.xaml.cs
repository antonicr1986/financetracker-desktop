using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FinanceTracker.Desktop.Localization;

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

        // Marca el idioma activo ahora y cada vez que cambie. Se deja de
        // escuchar al quitar la barra, porque el Localizer vive mas que ella.
        Loaded += (_, _) =>
        {
            App.Languages.Localizer.Changed += OnLanguageChanged;
            MarkActiveLanguage();
        };
        Unloaded += (_, _) => App.Languages.Localizer.Changed -= OnLanguageChanged;
    }

    private void UpdateSignOut()
    {
        SignOutButton.Command = SignOutCommand;
        SignOutButton.IsEnabled = SignOutCommand is not null;
    }

    private void OnLanguageChanged(object? sender, EventArgs e) => MarkActiveLanguage();

    private void MarkActiveLanguage()
    {
        var spanish = App.Languages.Localizer.Language == AppLanguage.Es;
        SpanishButton.Tag = spanish ? "Active" : null;
        EnglishButton.Tag = spanish ? null : "Active";
    }

    // Tema e idioma son cosa de la interfaz, no de ninguna pantalla en
    // concreto: los botones hablan directamente con los gestores de la aplicacion.
    private void OnThemeClick(object sender, RoutedEventArgs e) => App.Theme.Toggle();
    private void OnSpanishClick(object sender, RoutedEventArgs e) => App.Languages.Set(AppLanguage.Es);
    private void OnEnglishClick(object sender, RoutedEventArgs e) => App.Languages.Set(AppLanguage.En);
}
