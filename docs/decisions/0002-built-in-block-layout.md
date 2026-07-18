# ADR 0002: Organize built-in processing Blocks by functional directory

- **Status:** Accepted
- **Date:** 2026-07-18

## Context

DataProcesses will continue to add built-in processing Blocks after the initial release. These Blocks will cover signal sources, filters, spectral processing, visualizations, interoperability, and outputs. A flat `DataProcesses.Nodes.BuiltIn` project would make it difficult to identify the files, tests, settings, and documentation that belong to one user-visible Block.

The runtime plugin contract calls executable units `INode`, while the product uses **Block** to describe the user-visible processing unit in the Flow Editor. The terminology must remain compatible without introducing a second runtime abstraction.

## Decision

Each built-in Block is stored in its own PascalCase directory under:

```text
src/DataProcesses.Nodes.BuiltIn/Blocks/<BlockName>/
```

The standard Block consists of the following separate files:

| File | Responsibility |
|---|---|
| `<BlockName>Block.cs` | Stable type ID, port IDs, display metadata, category, and typed port contract. |
| `<BlockName>Node.cs` | Runtime execution logic implementing `INode`. |
| `<BlockName>NodeFactory.cs` | Factory that creates runtime instances. |
| `<BlockName>Settings.cs` | Optional immutable configuration and validation. |

Tests mirror the production directory under:

```text
tests/DataProcesses.Nodes.BuiltIn.Tests/Blocks/<BlockName>/
```

The root of `DataProcesses.Nodes.BuiltIn` is reserved for project-level composition. Each new Block factory must be registered in `BuiltInNodePlugin`, which exposes the catalog through `INodePlugin`.

## Consequences

The layout makes Block ownership, review scope, implementation, settings, tests, and documentation easier to locate. It provides a predictable pattern for GitHub Copilot and community contributors while allowing a Block to add focused files as it grows.

The project will have more directories and factory registration remains explicit. This is intentional: registration is a reviewable compatibility boundary and avoids hidden discovery behavior in the pre-alpha stage.
