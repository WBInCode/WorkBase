using System.Reflection;
using NetArchTest.Rules;
using WorkBase.Shared.Modules;
using Xunit;

namespace WorkBase.Tests.Architecture;

public class ModuleBoundaryTests
{
    // Sourced from the single ModuleCatalog (src/WorkBase.Shared/Modules/ModuleCatalog.cs)
    // so newly added modules are automatically covered by these isolation checks.
    private static readonly string[] ModuleNames = ModuleCatalog.All.Select(m => m.Namespace).ToArray();

    [Fact]
    public void Modules_ShouldNot_HaveDirectCrossReferences()
    {
        foreach (var sourceModule in ModuleNames)
        {
            var sourceAssembly = GetModuleAssembly(sourceModule, "Domain");
            if (sourceAssembly is null) continue;

            foreach (var targetModule in ModuleNames)
            {
                if (sourceModule == targetModule) continue;

                var targetNamespace = $"WorkBase.Modules.{targetModule}";

                var result = Types.InAssembly(sourceAssembly)
                    .ShouldNot()
                    .HaveDependencyOn(targetNamespace)
                    .GetResult();

                Assert.True(result.IsSuccessful,
                    $"Module {sourceModule}.Domain should not reference {targetModule}. " +
                    $"Offending types: {FormatTypes(result.FailingTypeNames)}");
            }
        }
    }

    [Fact]
    public void DomainLayer_ShouldNot_DependOn_ApplicationLayer()
    {
        foreach (var module in ModuleNames)
        {
            var domainAssembly = GetModuleAssembly(module, "Domain");
            if (domainAssembly is null) continue;

            var result = Types.InAssembly(domainAssembly)
                .ShouldNot()
                .HaveDependencyOn($"WorkBase.Modules.{module}.Application")
                .GetResult();

            Assert.True(result.IsSuccessful,
                $"{module}.Domain should not depend on {module}.Application. " +
                $"Offending types: {FormatTypes(result.FailingTypeNames)}");
        }
    }

    [Fact]
    public void DomainLayer_ShouldNot_DependOn_InfrastructureLayer()
    {
        foreach (var module in ModuleNames)
        {
            var domainAssembly = GetModuleAssembly(module, "Domain");
            if (domainAssembly is null) continue;

            var result = Types.InAssembly(domainAssembly)
                .ShouldNot()
                .HaveDependencyOn($"WorkBase.Modules.{module}.Infrastructure")
                .GetResult();

            Assert.True(result.IsSuccessful,
                $"{module}.Domain should not depend on {module}.Infrastructure. " +
                $"Offending types: {FormatTypes(result.FailingTypeNames)}");
        }
    }

    [Fact]
    public void ApplicationLayer_ShouldNot_DependOn_InfrastructureLayer()
    {
        foreach (var module in ModuleNames)
        {
            var appAssembly = GetModuleAssembly(module, "Application");
            if (appAssembly is null) continue;

            var result = Types.InAssembly(appAssembly)
                .ShouldNot()
                .HaveDependencyOn($"WorkBase.Modules.{module}.Infrastructure")
                .GetResult();

            Assert.True(result.IsSuccessful,
                $"{module}.Application should not depend on {module}.Infrastructure. " +
                $"Offending types: {FormatTypes(result.FailingTypeNames)}");
        }
    }

    [Fact]
    public void DomainLayer_ShouldNot_DependOn_ApiLayer()
    {
        foreach (var module in ModuleNames)
        {
            var domainAssembly = GetModuleAssembly(module, "Domain");
            if (domainAssembly is null) continue;

            var result = Types.InAssembly(domainAssembly)
                .ShouldNot()
                .HaveDependencyOn($"WorkBase.Modules.{module}.Api")
                .GetResult();

            Assert.True(result.IsSuccessful,
                $"{module}.Domain should not depend on {module}.Api. " +
                $"Offending types: {FormatTypes(result.FailingTypeNames)}");
        }
    }

    private static Assembly? GetModuleAssembly(string module, string layer)
    {
        var assemblyName = $"WorkBase.Modules.{module}.{layer}";
        try
        {
            return Assembly.Load(assemblyName);
        }
        catch
        {
            return null;
        }
    }

    private static string FormatTypes(IEnumerable<string>? types) =>
        types is null ? "none" : string.Join(", ", types);
}
