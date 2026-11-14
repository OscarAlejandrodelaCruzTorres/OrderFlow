using Asp.Versioning;
using Asp.Versioning.Conventions;

using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace OrderFlow.Api.Versioning;

public sealed class GlobalApiVersionConvention : IControllerConvention
{
    public bool Apply(IControllerConventionBuilder builder, ControllerModel controller)
    {
        if (controller.Attributes.OfType<ApiVersionNeutralAttribute>().Any())
            return false;

        builder.HasApiVersion(1.0);
        return true;

    }
}