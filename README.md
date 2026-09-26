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

There is a **public demo account**, `demo@financetracker.app` / `Demo1234!`,
shown on the sign-in screen. The API sleeps after 20 minutes of inactivity, so
the first sign-in of the day can take a while as the app service and the
database wake up — the window says so while it waits.

## ✨ What it does

- **Sign-in with JWT** against the deployed API, with clear messages for a
  wrong password and for a connection failure.
- **Month selector** with every month that has data, newest first.
- **Totals for the selected month** — income, expenses and balance — derived
  on the client from the full history, as in the other clients.
- **Transaction list** with category, date and a signed, coloured amount.
- **Loading, error and empty states**, with a retry button, and a clear
  message when the session expires instead of a silent return to sign-in.

## 🗺️ Next

Recording and editing transactions, budgets,
light and dark themes, Spanish and English, and a session that survives
closing the app.

## 🧰 Stack

- **.NET 8** and **WPF**
- **MVVM** with [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)
- **HttpClient** with `System.Net.Http.Json`
- **xUnit** for the tests

## 📁 Project structure

    FinanceTracker.Desktop/
      Domain/       Month grouping and totals — no WPF, plain C#
      Models/       The API DTOs, as records
      Services/     HTTP client, session and the error type
      ViewModels/   The logic of each screen — no reference to any control
      Views/        XAML windows, with almost empty code-behind
      App.xaml.cs   Creates the services and decides which window is shown
    FinanceTracker.Desktop.Tests/
                    ViewModel tests, with no window and no network

## 🧠 Decisions worth reading

**ViewModels do not open windows.** A ViewModel raises an event (`LoggedIn`,
`LoggedOut`) and `App.xaml.cs` does the navigation. That keeps the ViewModels
free of any reference to WPF windows, so they can be tested as plain classes.

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
bindable property. The window passes it to the ViewModel in a two-line
handler.

**A 401 means two different things.** On sign-in there is no session yet, so
it is a wrong password. On the dashboard there was a token and the API refused
it, so the session expired. The HTTP client is told which one applies at each
call.

**Dates are never converted.** The API sends `2026-09-01T00:00:00` with no time
zone, so .NET reads it as `DateTimeKind.Unspecified` and leaves it alone: the
1st of a month stays in that month wherever the user is.

**The timeout is 120 seconds, not the default 100.** Waking the free App
Service plan and the paused serverless database at the same time can take
longer than 100 seconds on the first request of the day.

## 🧪 Tests

`dotnet test` — 20 tests, with no window and no network.

Eleven cover the ViewModels: sign-in (empty fields, success, wrong password,
no connection) and the dashboard (newest month selected, changing month,
empty data, errors, expired session, signing out). Five cover the month
logic, including the 1st of a month and a list that crosses a year. Four check
the HTTP client against a fake `HttpMessageHandler`: every page is requested
with the token and `pageSize=100`, a single page is never followed by a second
request, and a 401 becomes "wrong password" or "session expired" depending on
the call.

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
