# AutoCAD Extensions: Platform Guide

This guide covers AutoCAD-specific patterns for writing extensions that integrate with Autodesk AutoCAD.

## Quick Reference

- **Extension interface:** `IAutoCADExtension<TArgs>`
- **Execution context:** `IAutoCADExtensionContext` with AutoCAD process document/database access
- **Database transactions:** Required for all model space modifications
- **Document locking:** Managed by integration layer
- **Supported versions:** AutoCAD 2020 and later

## Getting Started

See [Quick Start](../QUICK_START.md) for Args/Command basics. This guide covers AutoCAD-specific patterns.

## Document & Database Access

AutoCAD extensions work with the active document and its database.

### Database Transaction Pattern

*(Content to be added based on AutoCAD SDK patterns)*

## Selection & Filtering

*(Content to be added based on AutoCAD selection patterns)*

---

For comprehensive reference, see [Args Developer Guide](../ARGS_DEVELOPER_GUIDE.md).
