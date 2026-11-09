using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

using OrderFlow.Api.Authorization;

namespace OrderFlow.Api.Extensions;

public static class SecurityExtensions
{
    public static IServiceCollection AddApiSecurity(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();
        services.AddAuthorizationBuilder()
            // Endpoints are protected by default, so forgetting [Authorize] on a new one
            // cannot expose it; opening one up takes an explicit [AllowAnonymous].
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build())
            .AddPolicy(Policies.PlatformAdmin, policy => policy.RequireRole(Roles.PlatformAdmin));
        return services;
    }
}