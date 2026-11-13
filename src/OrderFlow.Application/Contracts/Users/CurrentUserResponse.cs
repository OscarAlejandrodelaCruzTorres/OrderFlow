namespace OrderFlow.Application.Contracts.Users;

public sealed record CurrentUserResponse(string? Sub, string? Name, IReadOnlyList<string> Roles);