using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace FinanceTracker.Desktop.Controls;

/// <summary>
/// Texto de ayuda para cajas de texto vacias (el "placeholder" de HTML, que
/// WPF no trae). Se usa asi:
///
///     &lt;TextBox controls:Hint.Text="tu@correo.com" /&gt;
///
/// Es una propiedad adjunta: una propiedad que se "pega" a un control que no
/// la tiene de serie. Las plantillas de TextBox y PasswordBox (Controls.xaml)
/// la pintan dentro de la caja, en un TextBlock llamado "Hint".
///
/// Donde empieza el texto dentro de una caja no es un numero fijo: WPF deja
/// un hueco propio antes del cursor que depende de la escala de pantalla. En
/// vez de adivinarlo (dos intentos fallidos), se mide la posicion real del
/// primer caracter y se coloca la ayuda justo ahi.
/// </summary>
public static class Hint
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.RegisterAttached(
        "Text", typeof(string), typeof(Hint), new PropertyMetadata(null, OnTextChanged));

    public static string? GetText(DependencyObject element) => (string?)element.GetValue(TextProperty);
    public static void SetText(DependencyObject element, string? value) => element.SetValue(TextProperty, value);

    /// <summary>
    /// Si la PasswordBox esta vacia. Hace falta porque PasswordBox no expone
    /// su contenido como propiedad (por seguridad) y un trigger no puede
    /// mirarlo; esta propiedad la mantiene al dia el evento PasswordChanged.
    /// </summary>
    public static readonly DependencyProperty IsEmptyProperty = DependencyProperty.RegisterAttached(
        "IsEmpty", typeof(bool), typeof(Hint), new PropertyMetadata(true));

    public static bool GetIsEmpty(DependencyObject element) => (bool)element.GetValue(IsEmptyProperty);

    /// <summary>
    /// Distancia entre el borde del area de texto y el primer caracter. Es la
    /// misma en TextBox y PasswordBox (usan el mismo motor de texto), pero solo
    /// TextBox permite medirla: se mide en la primera y se reutiliza.
    /// </summary>
    private static double? measuredInset;

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // Solo la primera vez que se asigna: engancharse una sola vez.
        if (e.OldValue is not null || d is not Control box) return;

        if (box is PasswordBox passwordBox)
            passwordBox.PasswordChanged += (_, _) =>
                passwordBox.SetValue(IsEmptyProperty, passwordBox.Password.Length == 0);

        // Se alinea cuando la caja ya esta dibujada.
        box.Loaded += (_, _) => AlignLater(box);
    }

    private static void AlignLater(Control box) =>
        // Loaded llega antes de que termine la maquetacion; se espera a que
        // WPF la acabe para que las posiciones sean las definitivas.
        box.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () => Align(box));

    private static void Align(Control box)
    {
        if (box.Template?.FindName("Hint", box) is not TextBlock hint ||
            box.Template.FindName("PART_ContentHost", box) is not FrameworkElement host ||
            !host.IsVisible)
            return;

        if (box is TextBox textBox)
        {
            // Posicion del primer caracter, relativa a la caja. Funciona aunque
            // este vacia: es donde se pintaria el cursor.
            var caret = textBox.GetRectFromCharacterIndex(0);
            if (!caret.IsEmpty)
            {
                var hostLeft = host.TranslatePoint(new Point(0, 0), textBox).X;
                measuredInset = caret.X - hostLeft;
            }
        }

        // Sin medida todavia (una PasswordBox sin ningun TextBox antes), el
        // hueco habitual de WPF a escala 100 %.
        var inset = measuredInset ?? 2;

        // La ayuda esta en la misma celda que el area de texto: mismo margen
        // que ella, mas el hueco medido a la izquierda.
        hint.Padding = new Thickness(0);
        hint.Margin = new Thickness(host.Margin.Left + inset, host.Margin.Top, host.Margin.Right, host.Margin.Bottom);
    }
}
