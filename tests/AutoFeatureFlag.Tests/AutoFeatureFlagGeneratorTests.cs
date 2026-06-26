using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace AutoFeatureFlag.Tests;

public class AutoFeatureFlagGeneratorTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────
    private static Dictionary<string, string> RunGenerator(string source)
    {
        var refs = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
        };
        try { refs.Add(MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location)); }
        catch { /* best-effort */ }

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [CSharpSyntaxTree.ParseText(source)],
            refs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new AutoFeatureFlagGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

        return driver.GetRunResult().GeneratedTrees
            .ToDictionary(
                t => System.IO.Path.GetFileName(t.FilePath),
                t => t.GetText().ToString());
    }

    private static IReadOnlyList<Diagnostic> GetDiagnostics(string source)
    {
        var refs = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
        };

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [CSharpSyntaxTree.ParseText(source)],
            refs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new AutoFeatureFlagGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);
        return diagnostics;
    }

    // ── Core types always emitted ─────────────────────────────────────────────
    [Fact]
    public void CoreTypes_AreAlwaysEmitted()
    {
        var files = RunGenerator("");
        files.Should().ContainKey("AutoFeatureFlag.Core.g.cs");
    }

    [Fact]
    public void CoreTypes_ContainsFeatureFlagsAttribute()
    {
        var files = RunGenerator("");
        files["AutoFeatureFlag.Core.g.cs"].Should().Contain("class FeatureFlagsAttribute");
    }

    [Fact]
    public void CoreTypes_ContainsIFeatureFlagProvider()
    {
        var files = RunGenerator("");
        files["AutoFeatureFlag.Core.g.cs"].Should().Contain("interface IFeatureFlagProvider");
    }

    [Fact]
    public void CoreTypes_ContainsInMemoryFeatureFlagProvider()
    {
        var files = RunGenerator("");
        files["AutoFeatureFlag.Core.g.cs"].Should().Contain("class InMemoryFeatureFlagProvider");
    }

    [Fact]
    public void CoreTypes_InMemoryProviderImplementsInterface()
    {
        var files = RunGenerator("");
        files["AutoFeatureFlag.Core.g.cs"].Should().Contain("InMemoryFeatureFlagProvider : IFeatureFlagProvider");
    }

    // ── Enum without members → AFF001 warning, no generated file ────────────
    [Fact]
    public void EmptyEnum_ReportsAFF001Warning()
    {
        var source = @"
using AutoFeatureFlag;
[FeatureFlags]
public enum Features { }";
        var diagnostics = GetDiagnostics(source);
        diagnostics.Should().ContainSingle(d => d.Id == "AFF001");
    }

    [Fact]
    public void EmptyEnum_DoesNotGenerateFlagsFile()
    {
        var source = @"
using AutoFeatureFlag;
[FeatureFlags]
public enum Features { }";
        var files = RunGenerator(source);
        files.Should().NotContainKey("AutoFeatureFlag.Features.g.cs");
    }

    // ── Single-member enum ────────────────────────────────────────────────────
    [Fact]
    public void SingleMemberEnum_GeneratesFlagsFile()
    {
        var source = @"
using AutoFeatureFlag;
[FeatureFlags]
public enum Features { DarkMode }";
        var files = RunGenerator(source);
        files.Should().ContainKey("AutoFeatureFlag.Features.g.cs");
    }

    [Fact]
    public void SingleMemberEnum_GeneratesInterface()
    {
        var source = @"
using AutoFeatureFlag;
[FeatureFlags]
public enum Features { DarkMode }";
        var output = RunGenerator(source)["AutoFeatureFlag.Features.g.cs"];
        output.Should().Contain("public interface IFeaturesFlags");
    }

    [Fact]
    public void SingleMemberEnum_GeneratesClass()
    {
        var source = @"
using AutoFeatureFlag;
[FeatureFlags]
public enum Features { DarkMode }";
        var output = RunGenerator(source)["AutoFeatureFlag.Features.g.cs"];
        output.Should().Contain("public sealed class FeaturesFlags : IFeaturesFlags");
    }

    [Fact]
    public void SingleMemberEnum_GeneratesIsEnabledProperty()
    {
        var source = @"
using AutoFeatureFlag;
[FeatureFlags]
public enum Features { DarkMode }";
        var output = RunGenerator(source)["AutoFeatureFlag.Features.g.cs"];
        output.Should().Contain("bool IsDarkModeEnabled { get; }");
        output.Should().Contain("public bool IsDarkModeEnabled => _provider.IsEnabled(\"DarkMode\");");
    }

    [Fact]
    public void SingleMemberEnum_ClassTakesProviderConstructorParam()
    {
        var source = @"
using AutoFeatureFlag;
[FeatureFlags]
public enum Features { DarkMode }";
        var output = RunGenerator(source)["AutoFeatureFlag.Features.g.cs"];
        output.Should().Contain("global::AutoFeatureFlag.IFeatureFlagProvider");
    }

    // ── Multi-member enum ────────────────────────────────────────────────────
    [Fact]
    public void MultipleMembersEnum_AllPropertiesGenerated()
    {
        var source = @"
using AutoFeatureFlag;
[FeatureFlags]
public enum AppFeatures { DarkMode, NewCheckout, BetaApi }";
        var output = RunGenerator(source)["AutoFeatureFlag.AppFeatures.g.cs"];
        output.Should().Contain("IsDarkModeEnabled");
        output.Should().Contain("IsNewCheckoutEnabled");
        output.Should().Contain("IsBetaApiEnabled");
    }

    // ── Namespaced enum ──────────────────────────────────────────────────────
    [Fact]
    public void NamespacedEnum_WrapsInNamespace()
    {
        var source = @"
using AutoFeatureFlag;
namespace MyApp.Features
{
    [FeatureFlags]
    public enum Toggles { PaywallBypass }
}";
        var output = RunGenerator(source)["AutoFeatureFlag.Toggles.g.cs"];
        output.Should().Contain("namespace MyApp.Features");
        output.Should().Contain("interface ITogglesFlags");
        output.Should().Contain("class TogglesFlags");
    }

    // ── No AFF001 for valid enum ─────────────────────────────────────────────
    [Fact]
    public void ValidEnum_NoAFF001Diagnostic()
    {
        var source = @"
using AutoFeatureFlag;
[FeatureFlags]
public enum Features { DarkMode }";
        var diagnostics = GetDiagnostics(source);
        diagnostics.Should().NotContain(d => d.Id == "AFF001");
    }

    // ── InMemoryFeatureFlagProvider runtime tests ────────────────────────────
    [Fact]
    public void InMemoryProvider_IsEnabled_ReturnsFalseByDefault()
    {
        var provider = new InMemoryFeatureFlagProvider();
        provider.IsEnabled("DarkMode").Should().BeFalse();
    }

    [Fact]
    public void InMemoryProvider_Enable_MakesIsEnabledTrue()
    {
        var provider = new InMemoryFeatureFlagProvider();
        provider.Enable("DarkMode");
        provider.IsEnabled("DarkMode").Should().BeTrue();
    }

    [Fact]
    public void InMemoryProvider_Disable_MakesIsEnabledFalse()
    {
        var provider = new InMemoryFeatureFlagProvider();
        provider.Enable("DarkMode");
        provider.Disable("DarkMode");
        provider.IsEnabled("DarkMode").Should().BeFalse();
    }

    [Fact]
    public void InMemoryProvider_SetEnabledTrue_MakesIsEnabledTrue()
    {
        var provider = new InMemoryFeatureFlagProvider();
        provider.SetEnabled("DarkMode", true);
        provider.IsEnabled("DarkMode").Should().BeTrue();
    }

    [Fact]
    public void InMemoryProvider_SetEnabledFalse_MakesIsEnabledFalse()
    {
        var provider = new InMemoryFeatureFlagProvider();
        provider.Enable("DarkMode");
        provider.SetEnabled("DarkMode", false);
        provider.IsEnabled("DarkMode").Should().BeFalse();
    }

    [Fact]
    public void InMemoryProvider_FlagsAreIndependent()
    {
        var provider = new InMemoryFeatureFlagProvider();
        provider.Enable("DarkMode");
        provider.IsEnabled("NewCheckout").Should().BeFalse();
        provider.IsEnabled("DarkMode").Should().BeTrue();
    }

    [Fact]
    public void InMemoryProvider_UnknownFlag_ReturnsFalse()
    {
        var provider = new InMemoryFeatureFlagProvider();
        provider.IsEnabled("DoesNotExist").Should().BeFalse();
    }

    // ── Generated file is auto-generated ─────────────────────────────────────
    [Fact]
    public void GeneratedFile_ContainsAutoGeneratedComment()
    {
        var source = @"
using AutoFeatureFlag;
[FeatureFlags]
public enum Features { DarkMode }";
        var output = RunGenerator(source)["AutoFeatureFlag.Features.g.cs"];
        output.Should().Contain("// <auto-generated by Swevo.AutoFeatureFlag/>");
    }

    [Fact]
    public void GeneratedFile_EnablesNullable()
    {
        var source = @"
using AutoFeatureFlag;
[FeatureFlags]
public enum Features { DarkMode }";
        var output = RunGenerator(source)["AutoFeatureFlag.Features.g.cs"];
        output.Should().Contain("#nullable enable");
    }
}
