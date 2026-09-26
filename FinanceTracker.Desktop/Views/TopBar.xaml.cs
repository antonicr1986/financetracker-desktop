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
            AttachToWindow();
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

    // ===== Barra de titulo propia =====

    private Window? window;

    private void AttachToWindow()
    {
        if (window is not null) return;
        window = Window.GetWindow(this);
        if (window is null) return;

        // Una ventana de tamaño fijo (el login) no se maximiza ni minimiza.
        var resizable = window.ResizeMode is ResizeMode.CanResize or ResizeMode.CanResizeWithGrip;
        MaximizeButton.Visibility = resizable ? Visibility.Visible : Visibility.Collapsed;
        MinimizeButton.Visibility = window.ResizeMode == ResizeMode.NoResize ? Visibility.Collapsed : Visibility.Visible;

        window.StateChanged += (_, _) => OnWindowStateChanged();
        OnWindowStateChanged();
    }

    private void OnWindowStateChanged()
    {
        if (window is null) return;
        var maximized = window.WindowState == WindowState.Maximized;

        // Icono de maximizar o de restaurar, como en Windows.
        MaximizeButton.Content = maximized ? "\uE923" : "\uE922";

        // Con WindowChrome, una ventana maximizada se sale de la pantalla por
        // cada lado lo que mide su borde de redimensionar (invisible). Sin
        // este margen se cortarian los bordes de la barra y del contenido.
        if (window.Content is FrameworkElement content)
            content.Margin = maximized ? SystemParameters.WindowResizeBorderThickness : new Thickness(0);
    }

    // SystemCommands: las mismas acciones que los botones de Windows.
    private void OnMinimizeClick(object sender, RoutedEventArgs e) => SystemCommands.MinimizeWindow(window!);

    private void OnMaximizeClick(object sender, RoutedEventArgs e)
    {
        if (window!.WindowState == WindowState.Maximized) SystemCommands.RestoreWindow(window);
        else SystemCommands.MaximizeWindow(window);
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => SystemCommands.CloseWindow(window!);

    // Tema e idioma son cosa de la interfaz, no de ninguna pantalla en
    // concreto: los botones hablan directamente con los gestores de la aplicacion.
    private void OnThemeClick(object sender, RoutedEventArgs e) => App.Theme.Toggle();
    private void OnSpanishClick(object sender, RoutedEventArgs e) => App.Languages.Set(AppLanguage.Es);
    private void OnEnglishClick(object sender, RoutedEventArgs e) => App.Languages.Set(AppLanguage.En);
}
