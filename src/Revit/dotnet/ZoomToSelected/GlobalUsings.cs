//-----------------------------------------------------------------------------
// GlobalUsings.cs
//
// This file defines all global namespace imports used throughout the 
// ZoomToSelected project. By centralizing these imports, we avoid
// having to add them in each individual file, improving code maintainability.
//
// Global using directives are available in C# 10 and later versions.
//-----------------------------------------------------------------------------

//-----------------------------------------------------------------------------
// .NET Core Framework namespaces
// These provide essential functionality for C# programming
//-----------------------------------------------------------------------------
global using System;                    // Core functionality like exceptions, base types
global using System.Linq;               // LINQ query extensions for collections
global using System.ComponentModel;     // For UI attributes like Description
global using System.ComponentModel.DataAnnotations; // Validation attributes like Required and Range
global using System.Threading;          // Threading utilities including cancellation
global using System.Threading.Tasks;    // Task-based asynchronous operations
global using System.Collections.Generic; // Generic collection types

//-----------------------------------------------------------------------------
// Assistant Extension Framework aliases
// Type aliases to simplify code readability
//-----------------------------------------------------------------------------
global using Result = CW.Assistant.Extensions.Contracts.Result; // Result type alias

//-----------------------------------------------------------------------------
// Assistant Extension Framework namespaces
// Core functionality for building extensions
//-----------------------------------------------------------------------------
global using CW.Assistant.Extensions.Contracts;      // Interfaces and base contracts
global using CW.Assistant.Extensions.Contracts.Enums; // Enumeration types
global using CW.Assistant.Extensions.Contracts.Collectors; // Data collection helpers
global using CW.Assistant.Extensions.Contracts.Attributes; // Attribute definitions for UI
global using CW.Assistant.Extensions.Contracts.Fields; // Strongly-typed field attributes for UI

//-----------------------------------------------------------------------------
// Revit-specific extension framework namespaces
// These provide functionality specific to Revit extensions
//-----------------------------------------------------------------------------
global using CW.Assistant.Extensions.Revit;           // Revit extension base classes
global using CW.Assistant.Extensions.Revit.Attributes; // Revit-specific UI attributes
global using CW.Assistant.Extensions.Revit.Collectors; // Revit data collection utilities

//-----------------------------------------------------------------------------
// Autodesk Revit API namespaces
// These provide access to the core Revit API functionality
//-----------------------------------------------------------------------------
global using Autodesk.Revit.DB;  // Revit database access (elements, parameters, etc.)
global using Autodesk.Revit.UI;  // Revit user interface interactions