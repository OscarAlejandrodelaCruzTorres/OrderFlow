using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;

using static ArchUnitNET.Fluent.ArchRuleDefinition;

// ArchUnitNET.Domain also has an Assembly type; this file means the reflection one.
using Assembly = System.Reflection.Assembly;

namespace OrderFlow.ArchitectureTests;

/// <summary>
/// The layer boundaries of ADR 001, checked on every build: a dependency added by mistake makes these tests
/// fail instead of relying on someone noticing it in review. They analyse compiled types, so a ProjectReference
/// that no code uses does not trip them; the first real use does.
/// </summary>
public sealed class LayerDependencyTests
{
    // By name, not typeof(...): Domain has no types yet to name.
    private static readonly Assembly DomainAssembly = Assembly.Load("OrderFlow.Domain");
    private static readonly Assembly ApplicationAssembly = Assembly.Load("OrderFlow.Application");
    private static readonly Assembly InfrastructureAssembly = Assembly.Load("OrderFlow.Infrastructure");
    private static readonly Assembly CompositionAssembly = Assembly.Load("OrderFlow.Composition");
    private static readonly Assembly ApiAssembly = Assembly.Load("OrderFlow.Api");

    // Built once and shared by every test: loading and analysing the assemblies is the expensive part.
    // Declared after the assemblies because static fields initialise in the order they appear.
    private static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(DomainAssembly, ApplicationAssembly, InfrastructureAssembly, CompositionAssembly, ApiAssembly)
        .Build();

    private static readonly IObjectProvider<IType> Domain =
        Types().That().ResideInAssembly(DomainAssembly).As("Domain");

    private static readonly IObjectProvider<IType> Application =
        Types().That().ResideInAssembly(ApplicationAssembly).As("Application");

    private static readonly IObjectProvider<IType> Infrastructure =
        Types().That().ResideInAssembly(InfrastructureAssembly).As("Infrastructure");

    private static readonly IObjectProvider<IType> Composition =
        Types().That().ResideInAssembly(CompositionAssembly).As("Composition");

    private static readonly IObjectProvider<IType> Api =
        Types().That().ResideInAssembly(ApiAssembly).As("Api");

    [Fact]
    public void DomainDependsOnNoOtherLayer() =>
        Types().That().Are(Domain)
            .Should().NotDependOnAny(Application)
            .AndShould().NotDependOnAny(Infrastructure)
            .AndShould().NotDependOnAny(Composition)
            .AndShould().NotDependOnAny(Api)
            // Domain has no types until F2; remove this then so the rule must check something again.
            .WithoutRequiringPositiveResults()
            .Check(Architecture);

    [Fact]
    public void ApplicationDoesNotDependOnInfrastructure() =>
        Types().That().Are(Application)
            .Should().NotDependOnAny(Infrastructure)
            .Check(Architecture);

    [Fact]
    public void ApiDoesNotDependOnInfrastructure() =>
        Types().That().Are(Api)
            .Should().NotDependOnAny(Infrastructure)
            .Check(Architecture);
}