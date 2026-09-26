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
- **Selector de mes** con todos los meses que tienen datos, del más reciente
  al más antiguo.
- **Totales del mes elegido** — ingresos, gastos y balance — calculados en el
  cliente sobre todo el historial, como en los otros clientes.
- **Lista de movimientos** con categoría, fecha e importe con signo y color.
- **Alta de movimientos** desde el panel, en un diálogo con tipo, descripción,
  importe, fecha y categoría. Solo se ofrecen las categorías del tipo elegido:
  la API rechaza un gasto con una categoría de ingreso. El importe acepta coma
  o punto para los decimales.
- **Edición y borrado de movimientos**: doble clic lo abre relleno en el mismo
  diálogo; borrar pide confirmación antes. Tras guardar, el panel se recarga y
  muestra el mes de ese movimiento.
- **Estados de carga, error y vacío**, con botón de reintentar, y un mensaje
  claro cuando la sesión caduca en lugar de volver al acceso sin explicación.

## 🗺️ Lo siguiente

Presupuestos, desglose por categoría,
temas claro y oscuro, español e inglés, y una sesión que sobreviva a cerrar la
aplicación.

## 🧰 Tecnologías

- **.NET 8** y **WPF**
- **MVVM** con [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)
- **HttpClient** con `System.Net.Http.Json`
- **xUnit** para las pruebas

## 📁 Estructura

    FinanceTracker.Desktop/
      Domain/       Agrupación por meses, totales y reglas del formulario — sin WPF
      Models/       Los DTOs de la API, como records
      Services/     Cliente HTTP, sesión, diálogos y el tipo de error
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

**Los ViewModels tampoco abren diálogos.** Preguntar "¿seguro?" con
`MessageBox` desde un ViewModel lo haría imposible de probar. Se lo piden a un
`IDialogService`: la implementación de la aplicación abre ventanas de verdad, y
la de las pruebas responde sí o no a demanda, así que "el usuario dijo que no y
no se borró nada" es una prueba unitaria normal.

**Un solo diálogo para crear y editar.** `TransactionEditorViewModel` recibe el
movimiento existente o nada. La misma ventana, la misma validación y los mismos
errores de la API, con el botón de borrar solo al editar — como en la web y en
Android.

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

**Un 401 significa dos cosas distintas.** En el acceso todavía no hay sesión,
así que es una contraseña incorrecta. En el panel había un token y la API lo
ha rechazado, así que la sesión ha caducado. Al cliente HTTP se le dice cuál
corresponde en cada llamada.

**Las fechas nunca se convierten.** La API envía `2026-09-01T00:00:00` sin zona
horaria, así que .NET la lee como `DateTimeKind.Unspecified` y no la toca: el
día 1 de un mes sigue en ese mes esté donde esté el usuario.

**El tiempo de espera es de 120 segundos, no los 100 por defecto.** Despertar
el plan gratuito de App Service y la base de datos serverless pausada a la vez
puede pasar de 100 segundos en la primera petición del día.

## 🧪 Pruebas

`dotnet test` — 49 pruebas, sin ventanas ni red.

Veintitrés cubren los ViewModels. Acceso: campos vacíos, éxito, contraseña
incorrecta, sin conexión. Panel: se elige el mes más reciente, cambio de mes,
sin datos, errores, sesión caducada, cierre de sesión, y que al guardar se
recarga y salta al mes del movimiento mientras que cancelar no recarga. El
diálogo de movimiento: valores iniciales de uno nuevo, categorías filtradas por
tipo, el cuerpo exacto que se envía, edición por id, borrar pregunta antes y no
hace nada si se dice que no, y los errores de la API dejan el diálogo abierto.

Trece cubren las reglas del formulario (importes con coma o punto, el
separador de miles rechazado por ambiguo, el orden en que se avisan los
problemas) y cinco la lógica de meses, incluido el día 1 y una lista que cruza
de año.

Ocho comprueban el cliente HTTP contra un `HttpMessageHandler` falso: se piden
todas las páginas con el token y `pageSize=100`, el JSON exacto de un
movimiento nuevo, `PUT` y `DELETE` respondidos con 204 sin cuerpo, un 404 y un
`category_type_mismatch` convertidos en su código, y un 401 que es "contraseña
incorrecta" o "sesión caducada" según la llamada.

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
