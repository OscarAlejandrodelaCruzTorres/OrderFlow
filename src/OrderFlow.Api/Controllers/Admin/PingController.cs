using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using OrderFlow.Api.Authorization;

namespace OrderFlow.Api.Controllers.Admin;

/// <summary>
/// Platform operations check: 204 only for PlatformAdmin. Version-neutral because /api/admin is an
/// operations surface, not part of the public contract; the policy sits on the class so every future admin
/// action inherits it.
/// </summary>
[ApiController]
[ApiVersionNeutral]
[Route("api/admin/ping")]
[Authorize(Policy = Policies.PlatformAdmin)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, "application/problem+json")]
public sealed class PingController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Get(CancellationToken cancellationToken = default) => NoContent();
}