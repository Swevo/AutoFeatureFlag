# Changelog

## [1.0.0] - 2026-06-26

### Added
- `[FeatureFlags]` attribute for marking enums as feature flag sets
- Compile-time generation of `I{EnumName}Flags` interface per decorated enum
- Compile-time generation of `{EnumName}Flags` sealed class implementing the interface
- `IFeatureFlagProvider` interface for pluggable flag providers
- `InMemoryFeatureFlagProvider` — thread-safe, in-process provider (defaults all flags to disabled)
- `AFF001` warning diagnostic when a `[FeatureFlags]` enum has no members
- Zero external dependencies; AOT-safe
