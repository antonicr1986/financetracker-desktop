using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FinanceTracker.Desktop.ViewModels;

/// <summary>
/// Primer ViewModel, solo para comprobar que el binding funciona.
/// No conoce ningun control de la ventana: solo expone datos y acciones.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    // [ObservableProperty] genera la propiedad publica "Message" y el aviso
    // a la vista cada vez que cambia (INotifyPropertyChanged).
    [ObservableProperty]
    private string message = "Todavia no has pulsado el boton";

    private int clicks;

    // [RelayCommand] genera "SayHelloCommand", que es lo que usa el boton.
    [RelayCommand]
    private void SayHello()
    {
        clicks++;
        Message = $"Has pulsado {clicks} veces";
    }
}
