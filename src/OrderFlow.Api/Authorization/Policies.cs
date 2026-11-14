namespace OrderFlow.Api.Authorization;

/// <summary>
/// Names of the authorization policies. Endpoints ask for a policy, never for a role, so what each policy
/// requires (a role today; a permission or a tenant later) changes in one place: AddApiSecurity.
/// </summary>
public static class Policies
{
    public const string PlatformAdmin = "PlatformAdmin";
}