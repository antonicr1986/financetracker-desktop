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

Hay una **cuenta de demostración pública**, la misma que usan la web y Android,
a un clic desde la pantalla de acceso. La API se duerme tras 20
minutos sin uso, así que el primer acceso del día puede tardar mientras se
despiertan el servicio y la base de datos — la ventana lo avisa mientras espera.

## ✨ Qué hace

- **Acceso con JWT** contra la API desplegada, con mensajes claros para una
  contraseña incorrecta y para un fallo de conexión.
- **Entrada a la cuenta de demostración con un clic**, sin rellenar el
  formulario.
- **Textos de ayuda dentro de las cajas vacías** (`tu@email.com`,
  `Tu contraseña`), que desaparecen en cuanto se escribe algo.
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
- **Temas claro y oscuro**, que se cambian desde la barra superior con los
  mismos iconos de luna y sol que la web, y se recuerdan entre sesiones.
  Mientras no se elige, la aplicación sigue el ajuste de Windows, como Android
  sigue el del teléfono, y lo sigue también si Windows cambia con la aplicación
  abierta. La barra de título también se oscurece.
- **Español e inglés**, que se cambian desde la barra superior con el mismo
  selector de dos botones que la web — bandera y código, el activo relleno — y
  se recuerdan entre sesiones. Mientras no se elige, la aplicación sigue el
  idioma de Windows, igual que la web sigue el del navegador. Todos los textos
  cambian a la vez, también un error que ya esté en pantalla, sin reiniciar y
  sin volver a llamar a la API. Los textos reutilizan las frases y claves de la
  web; importes y fechas siguen al idioma (`es-ES` / `en-GB`), siempre en euros.
- **La misma estética que la web**: la escala slate de Tailwind, tarjetas
  blancas sobre fondo gris (tarjetas slate sobre casi negro en oscuro), la
  acción principal en slate-900 (invertida en oscuro) y una barra superior
  común a todas las ventanas, con el nombre a la izquierda y el idioma, el
  tema y "Salir" a la derecha — "Salir" deshabilitado en la pantalla de acceso,
  como en los otros clientes.
- **Estados de carga, error y vacío**, con botón de reintentar, y un mensaje
  claro cuando la sesión caduca en lugar de volver al acceso sin explicación.

## 🗺️ Lo siguiente

Presupuestos, desglose por categoría, y una sesión que sobreviva a cerrar la
aplicación.

## 🧰 Tecnologías

- **.NET 8** y **WPF**
- **MVVM** con [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)
- **HttpClient** con `System.Net.Http.Json`
- **xUnit** para las pruebas

## 📁 Estructura

    FinanceTracker.Desktop/
      Domain/       Agrupación por meses, totales y reglas del formulario — sin WPF
      Localization/ Textos en los dos idiomas, el Localizer y el cambio de idioma
      Controls/     Hint.Text, el texto de ayuda de las cajas
      Models/       Los DTOs de la API, como records
      Services/     Cliente HTTP, sesión, diálogos y el tipo de error
      ViewModels/   La lógica de cada pantalla, sin referencias a controles
      Themes/       Light.xaml y Dark.xaml (colores), Controls.xaml (plantillas)
      Views/        Ventanas XAML y la barra superior común, con el code-behind casi vacío
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

**WPF no tiene placeholder, así que hay uno pequeño.** `Controls/Hint.cs` es una
propiedad adjunta — una propiedad que se puede poner en un control que no la
tiene — y se usa como `<TextBox controls:Hint.Text="tu@correo.com" />`. Las
plantillas de las cajas de texto y de contraseña la pintan dentro de la caja,
en la misma celda que el texto real. Dónde empieza el texto dentro de una caja
no es un número fijo — WPF deja su propio hueco antes del cursor, y cambia con
la escala de pantalla de Windows —, así que en vez de adivinarlo, `Hint` mide
dónde iría el primer carácter con `GetRectFromCharacterIndex(0)` y coloca la
ayuda justo ahí. Dos versiones anteriores adivinaban ese hueco y el cursor
quedaba entre la primera y la segunda letra de la ayuda. Como `PasswordBox` no deja ver su
contenido a los triggers, la propiedad adjunta también mantiene al día un
indicador `Hint.IsEmpty` desde `PasswordChanged`.

**Un 401 significa dos cosas distintas.** En el acceso todavía no hay sesión,
así que es una contraseña incorrecta. En el panel había un token y la API lo
ha rechazado, así que la sesión ha caducado. Al cliente HTTP se le dice cuál
corresponde en cada llamada.

**Las fechas nunca se convierten.** La API envía `2026-09-01T00:00:00` sin zona
horaria, así que .NET la lee como `DateTimeKind.Unspecified` y no la toca: el
día 1 de un mes sigue en ese mes esté donde esté el usuario.

**Un tema es un diccionario que se sustituye.** `Light.xaml` y `Dark.xaml`
definen las mismas claves (`Brush.Surface`, `Brush.Text`, `Brush.Income`…) con
los colores de la web. Al cambiar, uno reemplaza al otro en los recursos de la
aplicación, y todo lo que los lee con `{DynamicResource}` se repinta en el acto,
sin reiniciar ni parpadear. Hasta el icono que muestra el botón de tema es un
recurso (`Visibility.Moon`/`Visibility.Sun`), igual que la web lo decide en CSS
y no en código.

**Hubo que rehacer las plantillas de los controles de WPF.** Botones, cajas de
texto, desplegables, el selector de fecha, las filas de la lista y las barras
de desplazamiento vienen con el aspecto clásico de Windows y colores fijos, así
que en oscuro seguirían blancos. `Controls.xaml` da a cada uno una plantilla
pequeña que toma sus colores del tema, con las esquinas de 8 píxeles de la web.
La barra de título la pinta Windows, no WPF, así que se oscurece con
`DwmSetWindowAttribute`.

**Un idioma también es un diccionario que se sustituye.** `Strings.cs` tiene
todos los textos en los dos idiomas, en C#. `LanguageManager` convierte el
elegido en un diccionario de recursos y lo pone en el tercer hueco de la
aplicación, así que el XAML lee `{DynamicResource dashboard.income}` y se
repinta igual que con el tema. Los ViewModels obtienen los mismos textos de un
`Localizer` que reciben en el constructor, y escuchan su evento `Changed` para
rehacer lo que construyen ellos — nombres de meses, importes, fechas, el
saludo.

**Los errores se guardan como "cómo escribirlos", no como texto.** Un ViewModel
guarda una `Func<string>` para el error actual. Al cambiar de idioma se vuelve a
ejecutar, así que un mensaje en pantalla no se queda en el otro idioma.

**El inglés es `en-GB` con el euro fijado**, como en la web: `en-US` pondría el
mes antes que el día, y `en-GB` por sí solo mostraría libras.

**La decisión del tema es C# normal.** `ThemePreference` decide qué tema toca
(el elegido, o el de Windows si no hay elección) sin ninguna referencia a WPF,
así que tiene pruebas unitarias; `ThemeManager` solo pinta. La elección se
guarda en `%LocalAppData%\FinanceTracker\settings.json`, aparte de la sesión,
para que cerrar sesión no la reinicie.

**El tiempo de espera es de 120 segundos, no los 100 por defecto.** Despertar
el plan gratuito de App Service y la base de datos serverless pausada a la vez
puede pasar de 100 segundos en la primera petición del día.

## 🧪 Pruebas

`dotnet test` — 75 pruebas, sin ventanas ni red.

Veinticinco cubren los ViewModels. Acceso: campos vacíos, éxito, contraseña
incorrecta, sin conexión, y el botón de la demo, que usa sus credenciales sin
tocar ni exigir el formulario. Panel: se elige el mes más reciente, cambio de mes,
sin datos, errores, sesión caducada, cierre de sesión, y que al guardar se
recarga y salta al mes del movimiento mientras que cancelar no recarga. El
diálogo de movimiento: valores iniciales de uno nuevo, categorías filtradas por
tipo, el cuerpo exacto que se envía, edición por id, borrar pregunta antes y no
hace nada si se dice que no, y los errores de la API dejan el diálogo abierto.

Quince cubren los idiomas. Los dos diccionarios deben tener exactamente las
mismas claves y ningún texto vacío — olvidar una traducción rompe la
compilación, como en la web. Otra prueba lee los archivos XAML y comprueba que
cada clave de texto de `{DynamicResource}` existe, porque WPF muestra una que
falta como un texto vacío sin avisar. El resto comprueban los euros en los dos
idiomas (`12.345,60 €` y `€12,345.60`), que se sigue el idioma de Windows hasta
que se elige uno, y el cambio con las pantallas abiertas: el panel rehace sus
meses, importes y saludo sin volver a llamar a la API, un error en pantalla se
reescribe, y un diálogo cerrado deja de escuchar.

Siete cubren el tema: se sigue a Windows hasta que se elige, el cambio parte
de lo que se ve y se guarda, una vez elegido se ignora Windows, un valor
guardado desconocido cuenta como sin elegir, y el archivo de ajustes se guarda
y se lee bien y vuelve a los valores por defecto si falta o está roto.

Trece cubren las reglas del formulario (importes con coma o punto, el
separador de miles rechazado por ambiguo, el orden en que se avisan los
problemas), cinco la lógica de meses, incluido el día 1 y una lista que cruza de
año, y dos que un mes y una categoría muestran su nombre en un desplegable
cerrado y no el volcado por defecto del record.

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
