using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using OrderFlow.Api.Authorization;

namespace OrderFlow.Api.Controllers.Admin;

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
    public IActionResult Get() => NoContent();

}