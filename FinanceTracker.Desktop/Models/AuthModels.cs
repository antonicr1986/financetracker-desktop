namespace FinanceTracker.Desktop.Models;

// Copias de los DTOs de la API (FinanceTracker.Application/DTOs/Users).
// System.Text.Json las rellena sin distinguir mayusculas: "token" -> Token.

public record LoginRequest(string Email, string Password);

/// <summary>Cuerpo de POST /api/Users/register (RegisterUserDto).</summary>
public record RegisterRequest(string Name, string Email, string Password);

public record UserInfo(int Id, string Name, string Email);

public record LoginResponse(string Token, DateTime Expiration, UserInfo User);

/// <summary>Cuerpo de error de la API (ProblemDetails). Solo nos interesa el codigo.</summary>
public record ApiProblem(string? Code, string? Detail);
