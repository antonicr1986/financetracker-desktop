# 🖥️ FinanceTracker Desktop

[English](README.md) · **Español**

[![CI](https://img.shields.io/github/actions/workflow/status/antonicr1986/financetracker-desktop/ci.yml?branch=main&style=for-the-badge&label=CI&logo=githubactions&logoColor=white)](https://github.com/antonicr1986/financetracker-desktop/actions)
![.NET](https://img.shields.io/badge/.NET-8-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![WPF](https://img.shields.io/badge/WPF-MVVM-0078D4?style=for-the-badge&logo=windows&logoColor=white)

Cliente de escritorio para Windows de [FinanceTracker](https://github.com/antonicr1986/FinanceTracker),
una API REST de finanzas personales escrita en .NET 8.

**Es el tercer cliente de esa API**, después de
[financetracker-web](https://github.com/antonicr1986/financetracker-web) (Next.js)
y [financetracker-android](https://github.com/antonicr1986/financetracker-android)
(Kotlin). Cada uno llega a los mismos endpoints, códigos de error y reglas de
negocio desde una plataforma distinta. Este cierra el círculo en el mismo
lenguaje que la API: C# en los dos extremos.

> 🚧 **En desarrollo.** Se está construyendo paso a paso; la lista de abajo es
> lo que funciona hoy.

Hay una **cuenta de demostración pública**, `demo@financetracker.app` /
`Demo1234!`, que aparece en la pantalla de acceso. La API se duerme tras 20
minutos sin uso, así que el primer acceso del día puede tardar mientras se
despiertan el servicio y la base de datos — la ventana lo avisa mientras espera.

## ✨ Qué hace

- **Acceso con JWT** contra la API desplegada, con mensajes claros para una
  contraseña incorrecta y para un fallo de conexión.
- **Una ventana principal** que saluda al usuario y permite cerrar sesión.

## 🗺️ Lo siguiente

Movimientos y totales del mes, alta y edición de movimientos, presupuestos,
temas claro y oscuro, español e inglés, y una sesión que sobreviva a cerrar la
aplicación.

## 🧰 Tecnologías

- **.NET 8** y **WPF**
- **MVVM** con [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)
- **HttpClient** con `System.Net.Http.Json`
- **xUnit** para las pruebas

## 📁 Estructura

    FinanceTracker.Desktop/
      Models/       Los DTOs de la API, como records
      Services/     Cliente HTTP, sesión y el tipo de error
      ViewModels/   La lógica de cada pantalla, sin referencias a controles
      Views/        Ventanas XAML, con el code-behind casi vacío
      App.xaml.cs   Crea los servicios y decide qué ventana se ve
    FinanceTracker.Desktop.Tests/
                    Pruebas de los ViewModels, sin ventanas ni red

## 🧠 Decisiones que merece la pena leer

**Los ViewModels no abren ventanas.** Un ViewModel lanza un evento
(`LoggedIn`, `LoggedOut`) y es `App.xaml.cs` quien navega. Así los ViewModels
no tienen ninguna referencia a ventanas de WPF y se pueden probar como clases
normales.

**El ViewModel de acceso depende de una interfaz, no del cliente HTTP.**
`LoginViewModel` recibe un `IAuthService`. La aplicación le pasa el `ApiClient`
real; las pruebas, uno falso que responde al instante, y así se cubren todos
los casos de error sin servidor.

**Los errores se deciden por código, nunca por texto.** La API responde con
`ProblemDetails` y un `code` estable (`invalid_credentials`…). El cliente lo
convierte en una `ApiException` con ese código, y solo el ViewModel elige la
frase que se muestra — la misma regla que en la web y en Android.

**La contraseña es la única excepción a "nada en el code-behind".** WPF no
permite hacer binding de `PasswordBox.Password`, a propósito, para que la
contraseña no quede en una propiedad enlazable. La ventana se la pasa al
ViewModel en un manejador de dos líneas.

**El tiempo de espera es de 120 segundos, no los 100 por defecto.** Despertar
el plan gratuito de App Service y la base de datos serverless pausada a la vez
puede pasar de 100 segundos en la primera petición del día.

## 🧪 Pruebas

`dotnet test` — 6 pruebas de los ViewModels: campos vacíos (y que no se llama a
la API), acceso correcto (sesión iniciada, email recortado), credenciales
incorrectas, fallo de conexión y cierre de sesión.

## 🔄 Automatización

- **CI** en cada push y pull request, en `windows-latest` porque WPF solo
  compila en Windows: compilación, pruebas y la aplicación publicada,
  descargable desde la propia ejecución.
- El número de pruebas se escribe en el resumen de la ejecución, y **cero
  pruebas hace fallar el build**: unas pruebas que dejan de ejecutarse sin
  avisar son peores que unas en rojo.
- **Escaneo de secretos** con gitleaks sobre todo el historial, con la misma
  configuración que el resto de repositorios del proyecto y una regla más para
  credenciales escritas a mano en C#.

## ⚙️ Ejecutarlo en local

Windows, el SDK de .NET 8 y Visual Studio 2022 o cualquier editor.

    dotnet build
    dotnet test
    dotnet run --project FinanceTracker.Desktop

La URL de la API es `ApiClient.BaseUrl`, en `Services/ApiClient.cs`.

## ✍️ Autor

Antonio Company - [GitHub](https://github.com/antonicr1986) ·
[LinkedIn](https://www.linkedin.com/in/antoniocompany/)
