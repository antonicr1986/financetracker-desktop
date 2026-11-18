using FinanceTracker.Desktop.Models;

namespace FinanceTracker.Desktop.Services;

/// <summary>Activar y desactivar la verificacion en dos pasos (con sesion iniciada).</summary>
public interface IMfaService
{
    /// <summary>Genera el secreto (aun sin activar) y el enlace para el QR.</summary>
    Task<MfaSetupResponse> SetupMfaAsync();

    /// <summary>Activa el MFA con el primer codigo y devuelve los codigos de recuperacion.</summary>
    Task<MfaRecoveryCodesResponse> ConfirmMfaAsync(string code);

    Task DisableMfaAsync(string code);

    /// <summary>"Olvidar todos los dispositivos": el siguiente acceso pide el codigo en todos.</summary>
    Task ForgetDevicesAsync();
}
