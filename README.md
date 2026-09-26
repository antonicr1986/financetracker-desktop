# 🖥️ FinanceTracker Desktop

**English** · [Español](README.es.md)

[![CI](https://img.shields.io/github/actions/workflow/status/antonicr1986/financetracker-desktop/ci.yml?branch=main&style=for-the-badge&label=CI&logo=githubactions&logoColor=white)](https://github.com/antonicr1986/financetracker-desktop/actions)
![.NET](https://img.shields.io/badge/.NET-8-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![WPF](https://img.shields.io/badge/WPF-MVVM-0078D4?style=for-the-badge&logo=windows&logoColor=white)

Windows desktop client for [FinanceTracker](https://github.com/antonicr1986/FinanceTracker),
a personal finance REST API written in .NET 8.

**This is the third client of that API**, after
[financetracker-web](https://github.com/antonicr1986/financetracker-web) (Next.js)
and [financetracker-android](https://github.com/antonicr1986/financetracker-android)
(Kotlin). Each one reaches the same endpoints, error codes and business rules
from a different platform. This one closes the circle in the same language as
the API: C# on both ends.

> 🚧 **Work in progress.** It is being built step by step; the list below is
> what works today.

There is a **public demo account**, the same one the web and Android clients
use, reachable in one click from the sign-in screen. The API sleeps after 20 minutes of inactivity, so
the first sign-in of the day can take a while as the app service and the
database wake up — the window says so while it waits.

## ✨ What it does

- **Sign-in with JWT** against the deployed API, with clear messages for a
  wrong password and for a connection failure.
- **One-click entry into the demo account**, without filling in the form.
- **Hints inside the empty fields** (`tu@email.com`, `Tu contraseña`), which
  disappear as soon as something is typed.
- **Month selector** with every month that has data, newest first.
- **Totals for the selected month** — income, expenses and balance — derived
  on the client from the full history, as in the other clients.
- **Transaction list** with category, date and a signed, coloured amount.
- **Recording a transaction** from the dashboard, in a dialog with the type,
  description, amount, date and category. Only categories of the chosen type
  are offered: the API rejects an expense filed under an income category. The
  amount accepts a comma or a dot for decimals.
- **Editing and deleting a transaction**: a double click opens it prefilled in
  the same dialog; deleting asks for confirmation first. After saving, the
  dashboard reloads and shows the month of that transaction.
- **Light and dark themes**, switched from the top bar with the same moon and
  sun icons as the web client, and remembered between sessions. Until one is
  chosen the app follows the Windows setting, as the Android client follows
  the phone's — and keeps following it if Windows changes while the app is
  open. The title bar turns dark too.
- **The same look as the web client**: Tailwind's slate scale, white cards on
  a grey background (slate cards on near-black in dark mode), the main action
  in slate-900 (inverted in dark mode), and a shared top bar on every window
  with the name on the left and the theme switch and "Sign out" on the right —
  "Sign out" disabled on the sign-in screen, as in the other clients.
- **Loading, error and empty states**, with a retry button, and a clear
  message when the session expires instead of a silent return to sign-in.

## 🗺️ Next

Budgets, a breakdown by category, Spanish and English, and a session that survives
closing the app.

## 🧰 Stack

- **.NET 8** and **WPF**
- **MVVM** with [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)
- **HttpClient** with `System.Net.Http.Json`
- **xUnit** for the tests

## 📁 Project structure

    FinanceTracker.Desktop/
      Domain/       Month grouping, totals and form rules — no WPF, plain C#
      Models/       The API DTOs, as records
      Services/     HTTP client, session, dialogs and the error type
      ViewModels/   The logic of each screen — no reference to any control
      Themes/       Light.xaml and Dark.xaml (colours), Controls.xaml (templates)
      Views/        XAML windows and the shared top bar, with almost empty code-behind
      App.xaml.cs   Creates the services and decides which window is shown
    FinanceTracker.Desktop.Tests/
                    ViewModel tests, with no window and no network

## 🧠 Decisions worth reading

**ViewModels do not open windows.** A ViewModel raises an event (`LoggedIn`,
`LoggedOut`) and `App.xaml.cs` does the navigation. That keeps the ViewModels
free of any reference to WPF windows, so they can be tested as plain classes.

**ViewModels do not open dialogs either.** Asking "are you sure?" with
`MessageBox` from a ViewModel would make it untestable. They ask an
`IDialogService` instead: the app's implementation opens real windows, and the
tests' one answers yes or no on command, so "the user said no, nothing was
deleted" is a plain unit test.

**One dialog for creating and editing.** `TransactionEditorViewModel` receives
the existing transaction or nothing. The same window, the same validation and
the same API errors, with the delete button shown only when editing — as in the
web and Android clients.

**The sign-in ViewModel depends on an interface, not on the HTTP client.**
`LoginViewModel` receives an `IAuthService`. The app passes the real
`ApiClient`; the tests pass a fake that answers instantly, so every error case
can be covered without a server.

**Errors are decided by code, never by text.** The API answers with
`ProblemDetails` carrying a stable `code` (`invalid_credentials`…). The client
turns it into an `ApiException` with that code, and only the ViewModel picks
the sentence to show — the same rule as the web and Android clients.

**The password is the one exception to "no code-behind".** WPF does not allow
binding `PasswordBox.Password`, on purpose, so the password never sits in a
bindable property. The window passes it to the ViewModel in a short handler,
which also shows or hides the field's hint for the same reason.

**WPF has no placeholder.** There is no equivalent of HTML's `placeholder`
attribute, so each hint is a grey `TextBlock` laid over its box in the same
`Grid` cell, with `IsHitTestVisible="False"` so clicks go through to the box,
and a `DataTrigger` on `Text.Length` that shows it only while the box is empty.

**A 401 means two different things.** On sign-in there is no session yet, so
it is a wrong password. On the dashboard there was a token and the API refused
it, so the session expired. The HTTP client is told which one applies at each
call.

**Dates are never converted.** The API sends `2026-09-01T00:00:00` with no time
zone, so .NET reads it as `DateTimeKind.Unspecified` and leaves it alone: the
1st of a month stays in that month wherever the user is.

**A theme is a swapped dictionary.** `Light.xaml` and `Dark.xaml` define the
same keys (`Brush.Surface`, `Brush.Text`, `Brush.Income`…) with the web's
colours. Switching replaces one with the other in the application's resources,
and everything that reads them through `{DynamicResource}` repaints on the
spot, with no restart and no flash. Even which icon the theme button shows is
a resource (`Visibility.Moon`/`Visibility.Sun`), the way the web decides it in
CSS and not in code.

**WPF's own controls had to be re-templated.** Buttons, text boxes, drop-downs,
the date picker, list rows and scroll bars ship with the classic Windows
look and fixed colours, so in dark mode they would stay white. `Controls.xaml`
gives each one a small template that takes its colours from the theme, with
the web's 8-pixel corners. The title bar is drawn by Windows, not WPF, so it is
darkened through `DwmSetWindowAttribute`.

**The theme decision is plain C#.** `ThemePreference` decides which theme
applies (the saved choice, or Windows' setting when there is none) with no
reference to WPF, so it is unit-tested; `ThemeManager` only paints. The choice
is kept in `%LocalAppData%\FinanceTracker\settings.json`, apart from the
session, so signing out does not reset it.

**The timeout is 120 seconds, not the default 100.** Waking the free App
Service plan and the paused serverless database at the same time can take
longer than 100 seconds on the first request of the day.

## 🧪 Tests

`dotnet test` — 58 tests, with no window and no network.

Twenty-five cover the ViewModels. Sign-in: empty fields, success, wrong
password, no connection, and the demo button using the demo credentials
without touching or requiring the form. Dashboard: newest month selected, changing month,
empty data, errors, expired session, signing out, and that saving reloads and
jumps to the saved transaction's month while cancelling does not reload. The
transaction dialog: defaults for a new one, categories filtered by type, the
exact input sent, editing by id, delete asking first and doing nothing on "no",
and API errors keeping the dialog open.

Seven cover the theme: following Windows until a choice is made, toggling
from whatever is shown and saving it, ignoring Windows once chosen, an unknown
saved value counting as no choice, and the settings file round-tripping and
falling back to defaults when missing or broken.

Thirteen cover the form rules (amounts with a comma or a dot, a thousands
separator rejected as ambiguous, the order in which problems are reported) and
five the month logic, including the 1st of a month and a list that crosses a
year.

Eight check the HTTP client against a fake `HttpMessageHandler`: every page is
requested with the token and `pageSize=100`, the exact JSON body of a new
transaction, `PUT` and `DELETE` answered with a 204 and no body, a 404 and a
`category_type_mismatch` turned into their codes, and a 401 becoming "wrong
password" or "session expired" depending on the call.

## 🔄 Automation

- **CI** on every push and pull request, on `windows-latest` because WPF only
  builds on Windows: build, tests, and the published app, downloadable from the
  run itself.
- The test count is written to the run summary, and **zero tests fails the
  build**: tests that silently stop running are worse than red ones.
- **Secret scanning** with gitleaks across the full history, with the same
  configuration as the other repositories in this project plus a rule for
  credentials written by hand in C#.

## ⚙️ Running locally

Windows, the .NET 8 SDK, and Visual Studio 2022 or any editor.

    dotnet build
    dotnet test
    dotnet run --project FinanceTracker.Desktop

The API URL is `ApiClient.BaseUrl`, in `Services/ApiClient.cs`.

## ✍️ Author

Antonio Company - [GitHub](https://github.com/antonicr1986) ·
[LinkedIn](https://www.linkedin.com/in/antoniocompany/)
