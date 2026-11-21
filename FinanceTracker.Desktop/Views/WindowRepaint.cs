using System.Windows;
using System.Windows.Threading;

namespace FinanceTracker.Desktop.Views;

/// <summary>
/// Arreglo para ventanas que salian a medio pintar: transparentes, sin fondo ni
/// textos, y con solo lo que se volvia a dibujar despues (la caja con el foco,
/// el boton bajo el raton).
///
/// Todas las ventanas usan WindowChrome con GlassFrameThickness (sombra y
/// esquinas de Windows) y casi todas SizeToContent. Con esa combinacion, a veces
/// Windows muestra la ventana antes de que WPF termine el primer dibujo y lo que
/// no se vuelve a pintar se queda con el "cristal" del escritorio. Cambiar el
/// tamaño obliga a repintar la ventana entera, asi que, cuando ya se ha
/// mostrado, se ensancha un pixel y se deja como estaba.
/// </summary>
public static class WindowRepaint
{
    private static bool registered;

    /// <summary>Se aplica a todas las ventanas de la aplicacion, actuales y futuras.</summary>
    public static void Register()
    {
        if (registered) return;
        registered = true;

        EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent,
            new RoutedEventHandler((sender, _) =>
            {
                if (sender is Window window) window.ContentRendered += OnFirstRender;
            }));
    }

    private static void OnFirstRender(object? sender, EventArgs e)
    {
        if (sender is not Window window) return;
        window.ContentRendered -= OnFirstRender;

        // ContextIdle: cuando ya no queda nada pendiente de dibujar.
        window.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => Nudge(window)));
    }

    private static void Nudge(Window window)
    {
        // Maximizada o minimizada no hay tamaño que tocar: Windows la redibuja entera.
        if (window.WindowState != WindowState.Normal) return;

        // Tocar el ancho puede desactivar SizeToContent, que hace falta para que
        // la ventana siga creciendo con su contenido (un error que aparece...).
        var sizeToContent = window.SizeToContent;
        var width = window.ActualWidth;
        window.Width = width + 1;
        window.Width = width;
        window.SizeToContent = sizeToContent;
    }
}
