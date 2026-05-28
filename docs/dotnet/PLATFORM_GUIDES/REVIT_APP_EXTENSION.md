# Revit App Extension Guide

This guide is for Revit App Extensions that use a modeless app UI and Revit host APIs.

Read this with:

- [App Extension Developer Guide](../APP_EXTENSION_DEVELOPER_GUIDE.md)
- [Revit Platform Guide](./REVIT.md)
- [Args Developer Guide](../ARGS_DEVELOPER_GUIDE.md)

Common app architecture (DI, navigation shell, ViewModels, CQRS/service layering, design-time basics, cancellation/exception baseline) is documented in [App Extension Developer Guide](../APP_EXTENSION_DEVELOPER_GUIDE.md). This page only covers Revit-specific behavior.

## Entry Pattern

Keep `RevitAppExtensionCommand` startup-only:

1. validate `ActiveUIDocument`
2. build provider with `ServiceFactory.Create(context.UIApplication, ...)`
3. show the app window

Keep model operations out of the command entrypoint.

## Revit Context Rules

- Access document from `context.UIApplication.ActiveUIDocument?.Document`.
- Return a clear failure result when no model is open.
- Perform model changes inside `Transaction`.
- Keep transactions short and scoped.

## Query Handlers and Command Handlers

Revit App template is CQRS-first. Use this split:

- query handlers for read/result operations (`GetDocumentTitleQueryHandler`)
- command handlers for model changes (`DeleteSelectedElementsCommandHandler`)

When writing handlers:

- accept `CancellationToken`
- validate document/selection assumptions early
- keep transaction boundaries local to the operation
- return explicit result records for UI consumption

## Selection, Parameters, and ValueCopy

- Validate selection assumptions before execution.
- Verify parameter existence and storage type before writes.
- Use Revit collectors and `ValueCopy` when workflows need model-driven field values.

For write handlers, prefer partial success/result messages that explain what was processed vs skipped.

## Design-Time App Run and Design Query Handlers

Revit design startup has one key platform-specific switch:

- call `RegisterAppServices(..., useDesignQueryHandlers: true)` so CQRS resolves design handlers.

For CQRS operations, add Revit design handlers:

- `IDesignQueryHandler<TQuery, TResult>` for read workflows
- `IDesignCommandHandler<TCommand>` for write workflows

Design handlers should:

- return deterministic sample data
- avoid host API calls
- support fast iterative UI development

## Recommended Implementation Checklist

1. Add args fields and validation in `*Args.cs`.
2. Register Revit app services in `Registrations.cs` with CQRS enabled.
3. Keep `RevitAppExtensionCommand` startup-only.
4. Put all model writes in command handlers with explicit transaction scopes.
5. Add design query/command handlers and wire `useDesignQueryHandlers: true`.
6. Validate selection and parameter assumptions before writes.
7. Use partial-success style results for multi-element operations.
