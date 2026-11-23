using System.Reflection;

namespace FinanceTracker.Desktop.Services;

/// <summary>
/// Datos de la aplicacion instalada: los usan la pantalla de acceso (version
/// visible) y el cliente HTTP (cabecera X-Client-Version), asi que son siempre
/// la misma version.
/// </summary>
public static class AppInfo
{
    /// <summary>ID de la app en la Microsoft Store (es publico: sale en su URL).</summary>
    public const string StoreProductId = "9NT27R8DZ2DQ";

    /// <summary>
    /// La version sale de la etiqueta del release (-p:Version en el pipeline).
    /// .NET le anade "+&lt;commit&gt;" al final; solo se usa la parte de antes.
    /// </summary>
    public static string Version { get; } =
        (Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "?")
        .Split('+')[0];
}
