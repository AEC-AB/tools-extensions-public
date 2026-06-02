# Tekla App Extension Guide

This guide is for Tekla App Extensions that use a modeless app UI and Tekla host APIs.

Read this with:

- [App Extension Developer Guide](../APP_EXTENSION_DEVELOPER_GUIDE.md)
- [Tekla Platform Guide](./TEKLA.md)
- [Args Developer Guide](../ARGS_DEVELOPER_GUIDE.md)

Common app architecture (DI, navigation shell, ViewModels, CQRS/service layering, design-time basics, cancellation/exception baseline) is documented in [App Extension Developer Guide](../APP_EXTENSION_DEVELOPER_GUIDE.md). This page only covers Tekla-specific behavior.

## Entry Pattern

Keep `TeklaAppExtensionCommand` startup-only:

1. build provider with `ServiceFactory.Create(...)`
2. resolve main window handle
3. show the app window

Keep model operations out of the command entrypoint.

## Tekla Context Rules

- Create `Model` and verify connection with `GetConnectionStatus()` before model operations.
- Use selectors for user-driven object sets.
- Commit changes only after successful updates.

## App Services

Tekla App template is service-first. Keep Tekla operations in services/handlers, not in window code.

- write operations: explicit methods with clear success counts/results
- read operations: focused methods for UI state

Service authoring guidance:

- pass `CancellationToken` for long-running operations
- isolate selection iteration and update loops
- return simple result values that viewmodels can translate to UI messages
- commit model changes once per operation scope

## Selection and User Properties

- Validate selected object availability.
- Guard null/current object checks during iteration.
- Verify property names and expected value types before setting user properties.

For large selections, include progress-friendly operation design and cancellation checks.

## Design-Time App Run and Design Data

Tekla design startup is typically service-based and does not require CQRS design handler switching.

Design startup pattern:

1. create sample args
2. create provider with `ServiceFactory.Create(...)`
3. show `MainWindow`

If you need deterministic design-only host behavior, create a design service implementation (for example `IDesignTeklaService`) and register it only in design startup.

## Recommended Implementation Checklist

1. Add args fields and validation in `*Args.cs`.
2. Register Tekla app services in `Registrations.cs`.
3. Keep `TeklaAppExtensionCommand` startup-only.
4. Put model operations behind `ITeklaService` (or one equivalent service boundary).
5. Ensure selectors, null checks, and commit scope are explicit in each write operation.
6. Provide design startup data in `App.xaml.cs`.
7. Add optional design service registrations when UI should run without host dependencies.
