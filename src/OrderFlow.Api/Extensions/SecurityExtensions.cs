using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

using OrderFlow.Api.Authorization;

namespace OrderFlow.Api.Extensions;

/// <summary>
/// Authentication with JWT bearer tokens from an external identity provider (ADR 013) and authorization by
/// policies, with everything protected by default.
/// </summary>
public static class SecurityExtensions
{
    public static IServiceCollection AddApiSecurity(this IServiceCollection services)
    {
        // Settings come from Authentication:Schemes:Bearer. `dotnet user-jwts` fills them in for Development;
        // adopting an identity provider (ADR 013) only means setting Authority and ValidAudiences there.
        // With no settings every token is rejected.
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