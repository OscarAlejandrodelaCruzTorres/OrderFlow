namespace OrderFlow.Api.Authorization;

/// <summary>
/// Role names as they arrive in the token's role claim. They belong to the identity provider, so they are
/// literals that must match it exactly (never nameof: a rename must not change them); policies decide what
/// each role can do.
/// </summary>
public static class Roles
{
    public const string PlatformAdmin = "PlatformAdmin";
}