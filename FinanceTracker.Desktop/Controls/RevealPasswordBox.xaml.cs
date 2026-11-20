using System.Windows;
using System.Windows.Controls;

namespace FinanceTracker.Desktop.Controls;

/// <summary>
/// Caja de contraseña con un ojo para ver u ocultar lo escrito, como en la web
/// y en Android. WPF no lo trae: PasswordBox nunca muestra el texto. Por eso
/// hay dos cajas en el mismo sitio (una PasswordBox y un TextBox) y el ojo
/// alterna cual se ve, copiando el contenido de una a otra.
///
/// Se usa como la PasswordBox de antes: Password, el evento PasswordChanged
/// y Clear(). HintText es el texto de ayuda de la caja vacia.
/// </summary>
public partial class RevealPasswordBox : UserControl
{
    public static readonly DependencyProperty HintTextProperty = DependencyProperty.Register(
        nameof(HintText), typeof(string), typeof(RevealPasswordBox),
        new PropertyMetadata(null, OnHintTextChanged));

    // Evita que copiar de una caja a la otra dispare el evento dos veces.
    private bool syncing;

    public RevealPasswordBox()
    {
        InitializeComponent();
    }

    public string? HintText
    {
        get => (string?)GetValue(HintTextProperty);
        set => SetValue(HintTextProperty, value);
    }

    public string Password => HiddenInput.Password;

    public event RoutedEventHandler? PasswordChanged;

    public void Clear()
    {
        EyeButton.IsChecked = false;
        HiddenInput.Clear();
        VisibleInput.Clear();
    }

    private static void OnHintTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var box = (RevealPasswordBox)d;
        Hint.SetText(box.HiddenInput, (string?)e.NewValue);
        Hint.SetText(box.VisibleInput, (string?)e.NewValue);
    }

    private void OnHiddenChanged(object sender, RoutedEventArgs e)
    {
        if (syncing) return;
        PasswordChanged?.Invoke(this, e);
    }

    // Escribiendo con la contraseña a la vista: la PasswordBox se mantiene al
    // dia porque Password siempre se lee de ella.
    private void OnVisibleChanged(object sender, TextChangedEventArgs e)
    {
        if (syncing) return;
        syncing = true;
        HiddenInput.Password = VisibleInput.Text;
        syncing = false;
        PasswordChanged?.Invoke(this, e);
    }

    private void OnEyeChanged(object sender, RoutedEventArgs e)
    {
        var show = EyeButton.IsChecked == true;

        syncing = true;
        if (show) VisibleInput.Text = HiddenInput.Password;
        syncing = false;

        VisibleInput.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        HiddenInput.Visibility = show ? Visibility.Collapsed : Visibility.Visible;

        // Se sigue escribiendo donde se estaba, con el cursor al final.
        if (show)
        {
            VisibleInput.Focus();
            VisibleInput.CaretIndex = VisibleInput.Text.Length;
        }
        else
        {
            // No dejar la contraseña en claro en la caja oculta. Sin "syncing",
            // vaciarla vaciaria tambien la PasswordBox.
            syncing = true;
            VisibleInput.Clear();
            syncing = false;
            HiddenInput.Focus();
        }
    }
}
