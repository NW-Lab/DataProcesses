# DataProcesses repository instructions

DataProcesses is a pre-alpha, cross-platform C#/.NET desktop application for building data-processing flows and dashboards. Treat `docs/initial-specification.md` and accepted records in `docs/decisions/` as the product and architecture baseline. When code and documentation disagree, identify the mismatch rather than silently inventing behavior.

## Architecture boundaries

Keep `DataProcesses.Plugin.Abstractions` small, stable, and independent of Avalonia and implementation projects. Put graph validation, execution rules, and persistence-neutral domain behavior in `DataProcesses.Core`. Keep desktop presentation and Avalonia-specific code in `DataProcesses.Desktop`. Implement built-in nodes through the same contracts intended for third-party plugins.

Do not introduce a dependency from abstractions or core projects to the desktop project. Prefer explicit contracts and dependency injection over static global state. Do not add packages when a small implementation using the platform libraries is clear and maintainable.

## Data paths and terminology

Use the product terms **Flow Editor**, **App Settings (Preferences)**, **Fast Stream**, **JSON Message**, **node**, **port**, **dashboard**, and **widget** consistently.

Fast Stream transports high-rate numeric frames as typed timing metadata and channel buffers. Never serialize Fast Stream frames to CSV or JSON between processing nodes. Avoid per-sample allocation, boxing, LINQ, and copying in hot paths; process frames or spans and preserve cancellation and backpressure semantics.

CSV is for explicit import, export, storage, and interoperability nodes. The single-channel interchange shape may use `millis,data`; define whether time is relative or absolute. JSON Message carries versioned event, command, state, and metadata payloads. Do not place large sample arrays in JSON Message. Conversion between the two families requires an explicit node.

Port compatibility must validate direction, data family, and detailed schema. In UI designs, distinguish Fast Stream with a blue circular `S` port and solid connection, and JSON Message with an orange diamond `J` port and dashed connection. Do not rely on color alone.

## C# and .NET implementation

Keep nullable reference types enabled and preserve warnings-as-errors. Prefer immutable records for contracts and configuration snapshots. Validate constructor and public method inputs. Use asynchronous APIs only for genuinely asynchronous work and pass `CancellationToken` through execution boundaries.

Public contract changes require tests, documentation, and a compatibility note. Before adding or modifying a plugin contract, explain the intended versioning and migration path. Do not expose Avalonia, serializer-specific, or dependency-injection-container types through the public plugin API.

## Avalonia UI

Use MVVM and keep code-behind limited to view-only behavior that cannot reasonably be expressed through binding or behaviors. Place user-visible strings in localization resources rather than hard-coding them in views or view models. Design keyboard access, focus order, and non-color visual cues from the start. Keep Windows as the first packaging target without introducing Windows-only logic into shared UI or domain code.

## Tests and validation

For every behavior change, add or update deterministic tests. Node tests should use synthetic inputs and assert ports, validation, output frames/messages, cancellation, and error behavior. Add performance tests or benchmarks for material Fast Stream hot-path changes; do not claim performance improvements without measurements.

Before declaring work complete, run:

```bash
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
```

Keep docs and examples synchronized with code. Never commit build artifacts, credentials, personal data, biometric data, health data, or production recordings. Use generated test signals and synthetic fixtures.

## Change workflow

Read the relevant nearby code, tests, specification section, ADR, and path-specific instructions before editing. Make the smallest coherent change. If requirements are ambiguous, state assumptions and propose a narrow default. Do not redesign unrelated code.

When adding a node, load the `dataprocesses-node-development` Agent Skill. When asked for a repeatable implementation plan, use the matching prompt in `.github/prompts/` where available.
