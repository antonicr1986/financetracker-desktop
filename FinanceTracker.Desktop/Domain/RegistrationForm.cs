using System.Text.RegularExpressions;

namespace FinanceTracker.Desktop.Domain;

/// <summary>Lo primero que falla en el registro, en el orden de Android y la web.</summary>
public enum RegistrationProblem
{
    MissingFields,
    InvalidEmail,
    PasswordTooShort,
    PasswordsDontMatch,
}

/// <summary>Reglas del formulario de registro, en C# puro, como Validation.kt en Android.</summary>
public static partial class RegistrationForm
{
    /// <summary>MinLength(6) de RegisterUserDto en la API.</summary>
    public const int MinPasswordLength = 6;

    /// <summary>
    /// Forma basica de un correo: algo@algo.algo, sin espacios. La misma que en
    /// Android. No pretende ser exhaustiva; la API valida el resto.
    /// </summary>
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex Email();

    /// <summary>null si es valido. Espera nombre y correo ya recortados.</summary>
    public static RegistrationProblem? Validate(string name, string email, string password, string confirmation)
    {
        if (name.Length == 0 || email.Length == 0 || password.Length == 0) return RegistrationProblem.MissingFields;
        if (!Email().IsMatch(email)) return RegistrationProblem.InvalidEmail;
        if (password.Length < MinPasswordLength) return RegistrationProblem.PasswordTooShort;
        if (password != confirmation) return RegistrationProblem.PasswordsDontMatch;
        return null;
    }
}
