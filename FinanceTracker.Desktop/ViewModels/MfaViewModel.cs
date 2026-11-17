using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceTracker.Desktop.Localization;
using FinanceTracker.Desktop.Services;

namespace FinanceTracker.Desktop.ViewModels;

/// <summary>
/// Activar o desactivar la verificacion en dos pasos. Activarla: la API
/// genera el secreto (QR y clave), se confirma con el primer codigo de la app
/// y llegan los codigos de recuperacion, que solo se ven esa vez.
/// </summary>
public partial class MfaViewModel : ObservableObject, IDisposable
{
    private readonly IMfaService mfa;
    private readonly Session session;
    private readonly Localizer localizer;
    private Func<string>? error;

    public MfaViewModel(IMfaService mfa, Session session, Localizer localizer)
    {
        this.mfa = mfa;
        this.session = session;
        this.localizer = localizer;
        localizer.Changed += OnLanguageChanged;
    }

    public bool IsEnabled => session.User?.MfaEnabled ?? false;

    public string Status => localizer.T(IsEnabled ? "mfa.statusOn" : "mfa.statusOff");

    public string ActionText => localizer.T(IsEnabled ? "mfa.disable" : "mfa.enable");

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    [NotifyPropertyChangedFor(nameof(IsSetup))]
    [NotifyPropertyChangedFor(nameof(IsDisabling))]
    [NotifyPropertyChangedFor(nameof(IsShowingCodes))]
    private MfaStep step = MfaStep.Idle;

    public bool IsIdle => Step == MfaStep.Idle;
    public bool IsSetup => Step == MfaStep.Setup;
    public bool IsDisabling => Step == MfaStep.Disable;
    public bool IsShowingCodes => Step == MfaStep.Codes;

    /// <summary>Enlace otpauth:// que la ventana pinta como QR.</summary>
    [ObservableProperty]
    private string? otpAuthUri;

    /// <summary>Clave en grupos de 4, por si no se puede escanear el QR.</summary>
    [ObservableProperty]
    private string secret = "";

    [ObservableProperty]
    private string code = "";

    [ObservableProperty]
    private string recoveryCodes = "";

    [ObservableProperty]
    private string errorMessage = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    [NotifyCanExecuteChangedFor(nameof(DisableCommand))]
    private bool isBusy;

    /// <summary>401: la sesion ha caducado. La aplicacion vuelve al login.</summary>
    public event EventHandler? SessionExpired;

    private bool CanAct() => !IsBusy;

    /// <summary>El boton principal: activar o desactivar segun el estado.</summary>
    [RelayCommand(CanExecute = nameof(CanAct))]
    private async Task StartAsync()
    {
        if (IsEnabled)
        {
            Code = "";
            SetError(null);
            Step = MfaStep.Disable;
            return;
        }

        await RunAsync(async () =>
        {
            var setup = await mfa.SetupMfaAsync();
            OtpAuthUri = setup.OtpAuthUri;
            Secret = string.Join(" ", setup.Secret.Chunk(4).Select(group => new string(group)));
            Code = "";
            Step = MfaStep.Setup;
        });
    }

    [RelayCommand(CanExecute = nameof(CanAct))]
    private async Task ConfirmAsync()
    {
        if (Code.Trim().Length != 6) return;

        await RunAsync(async () =>
        {
            var result = await mfa.ConfirmMfaAsync(Code.Trim());
            session.SetMfaEnabled(true);
            RecoveryCodes = string.Join(Environment.NewLine, result.RecoveryCodes);
            ClearSetup();
            Step = MfaStep.Codes;
            RefreshStatus();
        });
    }

    [RelayCommand(CanExecute = nameof(CanAct))]
    private async Task DisableAsync()
    {
        if (Code.Trim().Length != 6) return;

        await RunAsync(async () =>
        {
            await mfa.DisableMfaAsync(Code.Trim());
            session.SetMfaEnabled(false);
            BackToIdle();
            RefreshStatus();
        });
    }

    /// <summary>Cancelar, o "ya los he guardado" tras ver los codigos.</summary>
    [RelayCommand]
    private void BackToIdle()
    {
        ClearSetup();
        RecoveryCodes = "";
        Code = "";
        SetError(null);
        Step = MfaStep.Idle;
    }

    private void ClearSetup()
    {
        OtpAuthUri = null;
        Secret = "";
        Code = "";
    }

    private async Task RunAsync(Func<Task> action)
    {
        SetError(null);
        IsBusy = true;
        try
        {
            await action();
        }
        catch (ApiException e) when (e.Code == ApiException.SessionExpired)
        {
            session.Clear();
            SessionExpired?.Invoke(this, EventArgs.Empty);
        }
        catch (ApiException e) when (e.Code is ApiException.MfaAlreadyEnabled or ApiException.MfaNotEnabled)
        {
            // La sesion se guardo antes de un cambio hecho en otro dispositivo.
            session.SetMfaEnabled(e.Code == ApiException.MfaAlreadyEnabled);
            BackToIdle();
            RefreshStatus();
        }
        catch (ApiException e)
        {
            SetError(() => localizer.ApiError(e.Code));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RefreshStatus()
    {
        OnPropertyChanged(nameof(IsEnabled));
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(ActionText));
    }

    private void SetError(Func<string>? text)
    {
        error = text;
        ErrorMessage = text?.Invoke() ?? "";
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        SetError(error);
        RefreshStatus();
    }

    public void Dispose() => localizer.Changed -= OnLanguageChanged;
}

public enum MfaStep
{
    Idle,
    Setup,
    Disable,
    Codes,
}
