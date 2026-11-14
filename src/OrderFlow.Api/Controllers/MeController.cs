using System.Security.Claims;

using Microsoft.AspNetCore.Mvc;

using OrderFlow.Application.Contracts.Users;

namespace OrderFlow.Api.Controllers;

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

    private CurrentUserResponse ToResponse() => new(
        Sub: User.FindFirstValue(ClaimTypes.NameIdentifier),
        Name: User.Identity?.Name,
        Roles: User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToList());
}