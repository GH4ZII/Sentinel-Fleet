using System.Reflection;
using NetArchTest.Rules;
using Xunit;

namespace SentinelFleet.ArchitectureTests;

public class LayerDependencyTests
{
    private static readonly Assembly DomainAssembly =
        typeof(Domain.DomainAssemblyMarker).Assembly;

    private static readonly Assembly ApplicationAssembly =
        typeof(Application.ApplicationAssemblyMarker).Assembly;

    private static readonly Assembly InfrastructureAssembly =
        typeof(Infrastructure.DependencyInjection).Assembly;

    private static readonly Assembly ApiAssembly = typeof(Program).Assembly;

    private static readonly Assembly[] ModuleAssemblies =
    [
        typeof(Modules.Identity.ModuleMarker).Assembly,
        typeof(Modules.Organizations.ModuleMarker).Assembly,
        typeof(Modules.Assets.ModuleMarker).Assembly,
        typeof(Modules.Devices.ModuleMarker).Assembly,
        typeof(Modules.Telemetry.ModuleMarker).Assembly,
        typeof(Modules.Geofences.ModuleMarker).Assembly,
        typeof(Modules.Rules.ModuleMarker).Assembly,
        typeof(Modules.Detections.ModuleMarker).Assembly,
        typeof(Modules.Incidents.ModuleMarker).Assembly,
        typeof(Modules.RiskScoring.ModuleMarker).Assembly,
        typeof(Modules.AIAnalysis.ModuleMarker).Assembly,
        typeof(Modules.Audit.ModuleMarker).Assembly,
    ];

    [Fact]
    public void Domain_Should_Not_Depend_On_Application_Infrastructure_Or_Api()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "SentinelFleet.Application",
                "SentinelFleet.Infrastructure",
                "SentinelFleet.Api")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailures(result));
    }

    [Fact]
    public void Application_Should_Not_Depend_On_Infrastructure_Or_Api()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "SentinelFleet.Infrastructure",
                "SentinelFleet.Api")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailures(result));
    }

    [Fact]
    public void Modules_Should_Not_Depend_On_Api()
    {
        foreach (var moduleAssembly in ModuleAssemblies)
        {
            var result = Types.InAssembly(moduleAssembly)
                .ShouldNot()
                .HaveDependencyOn("SentinelFleet.Api")
                .GetResult();

            Assert.True(result.IsSuccessful, $"{moduleAssembly.GetName().Name}: {FormatFailures(result)}");
        }
    }

    [Fact]
    public void Infrastructure_May_Depend_On_Domain_And_Application_But_Not_Api()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn("SentinelFleet.Api")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailures(result));
    }

    [Fact]
    public void Api_Assembly_Is_Loadable()
    {
        Assert.NotNull(ApiAssembly);
        Assert.Equal("SentinelFleet.Api", ApiAssembly.GetName().Name);
    }

    private static string FormatFailures(TestResult result)
    {
        if (result.FailingTypeNames is null || result.FailingTypeNames.Count == 0)
        {
            return "Architecture rule failed.";
        }

        return string.Join(", ", result.FailingTypeNames);
    }
}
