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

    /// <summary>
    /// "Cerrar sesion en todos los dispositivos": la API invalida todos los JWT
    /// de la cuenta, tambien el de este equipo.
    /// </summary>
    Task RevokeSessionsAsync();

    /// <summary>
    /// Cambia la contraseña. La API cierra todas las sesiones y devuelve un
    /// token nuevo para que este equipo siga dentro.
    /// </summary>
    Task<LoginResponse> ChangePasswordAsync(string currentPassword, string newPassword);
}
