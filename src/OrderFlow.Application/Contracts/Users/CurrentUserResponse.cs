namespace OrderFlow.Application.Contracts.Users;

/// <summary>
/// Who the caller is, as the API sees it after validating the token. Sub is the stable identifier to store;
/// Name is for display and may change.
/// </summary>
public sealed record CurrentUserResponse(string? Sub, string? Name, IReadOnlyList<string> Roles);