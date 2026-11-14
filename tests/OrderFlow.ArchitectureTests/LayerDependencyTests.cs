using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;

using static ArchUnitNET.Fluent.ArchRuleDefinition;

using Assembly = System.Reflection.Assembly;

namespace OrderFlow.ArchitectureTests;

public sealed class LayerDependencyTests
{
    private static readonly Assembly DomainAssembly = Assembly.Load("OrderFlow.Domain");
    private static readonly Assembly ApplicationAssembly = Assembly.Load("OrderFlow.Application");
    private static readonly Assembly InfrastructureAssembly = Assembly.Load("OrderFlow.Infrastructure");
    private static readonly Assembly CompositionAssembly = Assembly.Load("OrderFlow.Composition");
    private static readonly Assembly ApiAssembly = Assembly.Load("OrderFlow.Api");

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