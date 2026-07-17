# Swevo.AutoFeatureFlag

[![NuGet](https://img.shields.io/nuget/v/Swevo.AutoFeatureFlag.svg)](https://www.nuget.org/packages/Swevo.AutoFeatureFlag)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Swevo.AutoFeatureFlag.svg)](https://www.nuget.org/packages/Swevo.AutoFeatureFlag)
[![CI](https://github.com/Swevo/AutoFeatureFlag/actions/workflows/build.yml/badge.svg)](https://github.com/Swevo/AutoFeatureFlag/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

Compile-time feature flag stubs for .NET. Decorate an `enum` with `[FeatureFlags]` and get a strongly-typed interface and class — no reflection, no `Microsoft.FeatureManagement` overhead, AOT-safe.

```csharp
[FeatureFlags]
public enum AppFeatures
{
    DarkMode,
    NewCheckout,
    BetaApi
}

// ↓ Generator produces ↓

public interface IAppFeaturesFlags
{
    bool IsDarkModeEnabled   { get; }
    bool IsNewCheckoutEnabled { get; }
    bool IsBetaApiEnabled    { get; }
}

public sealed class AppFeaturesFlags : IAppFeaturesFlags
{
    public AppFeaturesFlags(IFeatureFlagProvider provider) ...
    public bool IsDarkModeEnabled    => provider.IsEnabled("DarkMode");
    public bool IsNewCheckoutEnabled => provider.IsEnabled("NewCheckout");
    public bool IsBetaApiEnabled     => provider.IsEnabled("BetaApi");
}
```

## Why not Microsoft.FeatureManagement?

| | `Microsoft.FeatureManagement` | `Swevo.AutoFeatureFlag` |
|---|---|---|
| Flag access | `await featureManager.IsEnabledAsync("DarkMode")` | `flags.IsDarkModeEnabled` |
| Typos | Runtime crash | Compile error |
| Dependencies | `Microsoft.FeatureManagement` + `Microsoft.Extensions.*` | **Zero** |
| AOT | ⚠️ reflection | ✅ |
| Test override | Configuration / snapshots | `provider.Enable("DarkMode")` |

## Quick Start

```bash
dotnet add package Swevo.AutoFeatureFlag
```

```csharp
// 1. Define your flags
[FeatureFlags]
public enum Features { DarkMode, BetaCheckout }

// 2. Wire up (choose your DI framework)
var provider = new InMemoryFeatureFlagProvider();
services.AddSingleton<IFeatureFlagProvider>(provider);
services.AddSingleton<IFeaturesFlags, FeaturesFlags>();

// 3. Inject and use
public class CheckoutService(IFeaturesFlags flags)
{
    public void Process()
    {
        if (flags.IsBetaCheckoutEnabled)
            RunBetaCheckout();
        else
            RunCheckout();
    }
}
```

## InMemoryFeatureFlagProvider

The bundled in-memory provider is thread-safe and defaults all flags to **disabled**.

```csharp
var provider = new InMemoryFeatureFlagProvider();

provider.Enable("DarkMode");           // → IsEnabled("DarkMode") == true
provider.Disable("DarkMode");          // → IsEnabled("DarkMode") == false
provider.SetEnabled("DarkMode", true); // same as Enable
```

### Testing

```csharp
public class CheckoutServiceTests
{
    private readonly InMemoryFeatureFlagProvider _provider = new();
    private readonly FeaturesFlags _flags;

    public CheckoutServiceTests() => _flags = new FeaturesFlags(_provider);

    [Fact]
    public void BetaCheckout_WhenEnabled_UsesBetaPath()
    {
        _provider.Enable("BetaCheckout");
        var sut = new CheckoutService(_flags);
        // assert beta path taken ...
    }
}
```

## Custom Providers

Implement `IFeatureFlagProvider` to read from any source — configuration, database, LaunchDarkly, etc.:

```csharp
public sealed class ConfigFeatureFlagProvider(IConfiguration config) : IFeatureFlagProvider
{
    public bool IsEnabled(string flagName)
        => config.GetValue<bool>($"FeatureFlags:{flagName}");

    public void Enable(string flagName)  => throw new NotSupportedException("Use appsettings.json");
    public void Disable(string flagName) => throw new NotSupportedException("Use appsettings.json");
    public void SetEnabled(string flagName, bool enabled) => throw new NotSupportedException();
}
```

## Diagnostics

| ID | Severity | Description |
|---|---|---|
| `AFF001` | Warning | `[FeatureFlags]` enum has no members — no code is generated |

## Part of the Swevo ecosystem

| Package | Purpose |
|---|---|
| [Swevo.AutoFeatureFlag](https://github.com/Swevo/AutoFeatureFlag) | This package |
| [Swevo.AutoResult](https://github.com/Swevo/AutoResult.Generator) | Result&lt;T&gt; + TryWrap wrappers |
| [Swevo.AutoAudit](https://github.com/Swevo/AutoAudit) | Audit fields (CreatedAt, UpdatedAt) |
| [Swevo.EFCore.SoftDelete](https://github.com/Swevo/EFCore.SoftDelete) | Soft delete + global query filters |
| [Swevo.EFCore.StronglyTyped](https://github.com/Swevo/EFCore.StronglyTyped) | Strongly-typed IDs for EF Core |
| [Swevo.EFCore.Outbox](https://github.com/Swevo/EFCore.Outbox) | Transactional outbox pattern |
| [AutoLog.Generator](https://github.com/Swevo/AutoLog.Generator) | Compile-time high-performance `LoggerMessage.Define` logging |
| [AutoHttpClient.Generator](https://github.com/Swevo/AutoHttpClient.Generator) | Compile-time typed HTTP client. AOT-safe Refit alternative |

## Related Packages

| Package | Downloads | Description |
|---|---|---|
| [Swevo.AutoBus](https://www.nuget.org/packages/Swevo.AutoBus) | [![Downloads](https://img.shields.io/nuget/dt/Swevo.AutoBus.svg)](https://www.nuget.org/packages/Swevo.AutoBus) | Free, MIT-licensed in-process message bus for  |
| [Swevo.AutoBus.RabbitMQ](https://www.nuget.org/packages/Swevo.AutoBus.RabbitMQ) | [![Downloads](https://img.shields.io/nuget/dt/Swevo.AutoBus.RabbitMQ.svg)](https://www.nuget.org/packages/Swevo.AutoBus.RabbitMQ) | RabbitMQ transport for AutoBus |
| [Swevo.AutoAssert](https://www.nuget.org/packages/Swevo.AutoAssert) | [![Downloads](https://img.shields.io/nuget/dt/Swevo.AutoAssert.svg)](https://www.nuget.org/packages/Swevo.AutoAssert) | Free, MIT-licensed fluent assertions for  |
| [Swevo.AutoAuth](https://www.nuget.org/packages/Swevo.AutoAuth) | [![Downloads](https://img.shields.io/nuget/dt/Swevo.AutoAuth.svg)](https://www.nuget.org/packages/Swevo.AutoAuth) | A free, MIT-licensed fluent configuration wrapper around OpenIddict for building OAuth2/OIDC token servers in ASP |
| [Swevo.AutoAudit](https://www.nuget.org/packages/Swevo.AutoAudit) | [![Downloads](https://img.shields.io/nuget/dt/Swevo.AutoAudit.svg)](https://www.nuget.org/packages/Swevo.AutoAudit) | Compile-time audit field generation for EF Core entities using Roslyn source generators |
| [Swevo.AutoResult](https://www.nuget.org/packages/Swevo.AutoResult) | [![Downloads](https://img.shields.io/nuget/dt/Swevo.AutoResult.svg)](https://www.nuget.org/packages/Swevo.AutoResult) | Compile-time Result<T> monad for  |
| [Swevo.AutoGuard](https://www.nuget.org/packages/Swevo.AutoGuard) | [![Downloads](https://img.shields.io/nuget/dt/Swevo.AutoGuard.svg)](https://www.nuget.org/packages/Swevo.AutoGuard) | Compile-time guard clauses for  |
| [Swevo.AutoImage](https://www.nuget.org/packages/Swevo.AutoImage) | [![Downloads](https://img.shields.io/nuget/dt/Swevo.AutoImage.svg)](https://www.nuget.org/packages/Swevo.AutoImage) | A free, MIT-licensed fluent image processing wrapper around SkiaSharp for  |
| [Swevo.AutoTestData](https://www.nuget.org/packages/Swevo.AutoTestData) | [![Downloads](https://img.shields.io/nuget/dt/Swevo.AutoTestData.svg)](https://www.nuget.org/packages/Swevo.AutoTestData) | Compile-time test data builders for  |

---

## License

MIT © 2026 Justin Bannister
