using CommunityToolkit.Mvvm.ComponentModel;

namespace FinanceTracker.Desktop.ViewModels;

/// <summary>
/// Aviso de "el servidor esta despertando", igual que en la web y Android.
///
/// La API corre en el plan gratuito de Azure, que se descarga tras un rato sin
/// uso, y la base de datos serverless se pausa. El backend reintenta solo, pero
/// desde fuera solo se ve un boton quieto: si la espera se alarga, se explica
/// por que en lugar de dejar que parezca que esta roto. Solo aparece pasados
/// unos segundos, para no molestar cuando la API ya esta despierta.
/// </summary>
public sealed partial class WakingNotice : ObservableObject
{
    /// <summary>El mismo retraso que la web y Android.</summary>
    public static readonly TimeSpan DefaultDelay = TimeSpan.FromSeconds(4);

    private readonly TimeSpan delay;
    private CancellationTokenSource? timer;

    public WakingNotice(TimeSpan? delay = null) => this.delay = delay ?? DefaultDelay;

    [ObservableProperty]
    private bool isVisible;

    /// <summary>Empieza a contar al enviar la peticion.</summary>
    public void Start()
    {
        Stop();
        timer = new CancellationTokenSource();
        _ = ShowAfterDelayAsync(timer.Token);
    }

    /// <summary>Al terminar la peticion (bien o mal): se oculta y deja de contar.</summary>
    public void Stop()
    {
        timer?.Cancel();
        timer?.Dispose();
        timer = null;
        IsVisible = false;
    }

    private async Task ShowAfterDelayAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(delay, token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        // En WPF la continuacion se encola en el hilo de la interfaz: si la
        // peticion termino justo antes, Stop ya ha cancelado y no se muestra.
        if (!token.IsCancellationRequested) IsVisible = true;
    }
}
