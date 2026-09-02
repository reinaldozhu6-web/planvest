using System.ComponentModel.DataAnnotations;

namespace PlanVest.Api.Contracts;

public sealed record RegisterRequest(
    [property: Required, StringLength(80, MinimumLength = 2)] string DisplayName,
    [property: Required, EmailAddress, StringLength(254)] string Email,
    [property: Required, StringLength(128, MinimumLength = 10)] string Password);

public sealed record LoginRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required] string Password);

public sealed record AuthResponse(string AccessToken, DateTimeOffset ExpiresAt, UserResponse User);
public sealed record UserResponse(Guid Id, string DisplayName, string Email);
