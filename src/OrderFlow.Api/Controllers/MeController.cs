using System.Security.Claims;

using Microsoft.AspNetCore.Mvc;

using OrderFlow.Application.Contracts.Users;

namespace OrderFlow.Api.Controllers;

/// <summary>
/// The caller's own identity, read only from the validated token. No policy of its own: the fallback policy
/// (authenticated user) is enough. Versions come from GlobalApiVersionConvention.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/me")]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, "application/problem+json")]
public sealed class MeController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    public ActionResult<CurrentUserResponse> GetCurrentUser(CancellationToken cancellationToken = default) => Ok(ToResponse());

    // ClaimTypes, not "sub" or "role": JwtBearer maps the short JWT claim names on the way in. Named arguments
    // because Sub and Name are both strings and a swapped order would still compile.
    private CurrentUserResponse ToResponse() => new(
        Sub: User.FindFirstValue(ClaimTypes.NameIdentifier),
        Name: User.Identity?.Name,
        Roles: User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToList());
}