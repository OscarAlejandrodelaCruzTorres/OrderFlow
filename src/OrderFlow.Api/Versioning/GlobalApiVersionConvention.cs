using Asp.Versioning;
using Asp.Versioning.Conventions;

using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace OrderFlow.Api.Versioning;

/// <summary>
/// Declares the API versions once for every versioned controller, so a new version cannot be forgotten in one
/// of them; a method that changes between versions still uses MapToApiVersion. Operations endpoints
/// (/api/admin) stay version-neutral.
/// </summary>
public sealed class GlobalApiVersionConvention : IControllerConvention
{
    public bool Apply(IControllerConventionBuilder builder, ControllerModel controller)
    {
        if (controller.Attributes.OfType<ApiVersionNeutralAttribute>().Any())
        {
            return false;
        }

        builder.HasApiVersion(1.0);
        return true;
    }
}