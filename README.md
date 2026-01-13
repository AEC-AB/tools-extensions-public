# tools-extensions-public
![Assistant by AEC Logo](./Assistant-by-AEC-full-logo.svg)
A shared, community-driven collection of Assistant extensions.

## What is Assistant by AEC?

Assistant is an extensible automation and orchestration platform for the AEC (Architecture, Engineering, and Construction) industry. It standardizes, automates, and operationalizes BIM- and model-based workflows across tools such as Revit, Tekla Structures, AutoCAD, Navisworks, Solibri, Dynamo, and related ecosystems. The core value is **standardization made executable**—turning approved ways of working into repeatable, auditable workflows that anyone can run.

Assistant workflows can run manually, scheduled, or event-based, and span multiple applications within a single flow.

Automation here is more than eliminating manual clicks. By encoding approved methods into executable steps, teams get consistent outcomes, higher quality, and less dependency on a handful of key people to remember how things should be done.

## How Assistant is put together

- Core platform: Loads and executes workflows, manages configuration and context, and orchestrates calls to integrations. It is tool-agnostic and not part of this repository.
- Integrations (host apps): Thin layers that know how to talk to applications such as Revit, Tekla Structures, AutoCAD, Navisworks, and others, exposing a consistent execution model to the core. They may run in-process or out-of-process depending on the host.
- Extensions (this repo): Small, focused units of functionality bound to a specific integration and tech stack. They trigger native commands, read/validate/modify model data, export files (IFC, DWG, reports), enforce standards, automate coordination tasks, or bridge data between tools. Assistant composes these into larger workflows.

## What lives in this repository

- Public, open-source extensions for Assistant—generic, reusable building blocks meant to be dropped into Assistant workflows.
- No company-specific or project-specific logic.
- Reference implementations that show how to build Assistant extensions for common AEC tools.

## Repository layout

Extensions follow a consistent convention:

```
Integration/
└── TechStack/
    └── Extension/
```


Each extension folder typically contains:
- Source code for the action/command.
- A minimal project file and build configuration for the target host.
- A README describing usage, inputs/outputs, and any host-specific notes.

Assistant itself is **not open source**; only the extensions are.

## Who this repo is for

- Developers building Assistant extensions.
- Technical BIM specialists and automation engineers.
- Contributors who want to create reusable, shareable functionality for the Assistant platform.

## Contributing

Contributions are welcome. Please open an issue or pull request if you want to improve an existing extension or contribute a new generic extension. Keep contributions generic and reusable, avoid company- or project-specific logic, and follow the established folder and coding conventions.

## Download Assistant & start a trial

To learn more about Assistant, download the application, or start a free trial, visit **https://assistant.aec.se/**.

