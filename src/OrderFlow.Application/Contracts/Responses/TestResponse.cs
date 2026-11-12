
namespace OrderFlow.Application.Contracts.Responses;

public sealed record TestResponse(string? Name, IReadOnlyList<string> Roles)
{
}