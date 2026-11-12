using System.Security.Claims;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using OrderFlow.Api.Authorization;
using OrderFlow.Application.Contracts.Responses;

namespace OrderFlow.Api.Controllers.Test;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/test")]
public sealed class TestController : Controller
{
    [HttpGet]
    [ProducesResponseType<TestResponse>(StatusCodes.Status200OK)]
    public ActionResult<TestResponse> Get() => Ok(ToResponse());


    [HttpGet("admin")]
    [ProducesResponseType<TestResponse>(StatusCodes.Status200OK)]
    [Authorize(Policy = Policies.PlatformAdmin)]
    public ActionResult<TestResponse> GetAdmin() => Ok(ToResponse());

    // ClaimTypes.Role, not "role": JwtBearer maps the short JWT claim names to ClaimTypes on the way in.
    private TestResponse ToResponse() => new(
        User.Identity?.Name,
        User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToList());
}