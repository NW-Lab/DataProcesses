# Contributing to DataProcesses

Thank you for helping build DataProcesses. The project is in a pre-alpha design and scaffolding stage, so proposals that clarify interfaces, performance constraints, accessibility, and cross-platform behavior are especially valuable.

## Before starting

For a small bug fix, open a focused pull request directly. For a new node type, public contract change, persistence-format change, or architectural change, open an issue or discussion first. This prevents incompatible implementations from forming around an interface that is still evolving.

| Change | Expected preparation |
|---|---|
| Documentation or small bug fix | Focused pull request |
| New built-in node | Issue describing ports, settings, and test data |
| Public plugin contract | Issue plus an Architecture Decision Record |
| Project-file schema | Migration and backward-compatibility plan |
| Fast Stream hot path | Benchmark or measurable performance evidence |
| User-facing text | English resource key and localization-ready wording |

## Local setup

Use the SDK selected by `global.json`, then run the following commands from the repository root:

```bash
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
```

Warnings are treated as errors. Keep nullable reference types enabled and do not suppress analyzers without documenting why.

## Branches and commits

Create a short-lived branch from the default branch. Use a concise branch name such as `feature/filter-node` or `fix/port-validation`. Commits should describe the reason for the change rather than merely listing edited files.

## Pull requests

A pull request should explain the problem, the chosen approach, and the validation performed. Keep unrelated refactoring separate. If the change affects the UI, include screenshots where practical. If it affects the public plugin API, explain source and binary compatibility implications.

Before requesting review, confirm that the solution builds, all tests pass, new behavior has tests, documentation matches the implementation, and no generated artifacts or credentials are included.

## Architecture rules

`DataProcesses.Plugin.Abstractions` must remain small and independent of Avalonia. `DataProcesses.Core` must not reference desktop UI types. `DataProcesses.Desktop` may compose the core and UI, but business rules belong in the core. Built-in nodes use the same public contracts intended for third-party nodes.

Fast Stream processing should use typed buffers and frame-level operations. Do not convert each frame to CSV or JSON inside the processing graph. CSV is intended for import, export, and explicit interoperability nodes. JSON Message is intended for variable event and control payloads, not high-rate sample arrays.

## Adding a node

A new node should have one clear responsibility. Define stable port identifiers, directions, data kinds, and detailed schemas. Validate configuration before execution. Handle cancellation and cleanup. Add deterministic tests with known input and expected output. Update documentation when users need to understand the node.

The repository includes GitHub Copilot instructions and a reusable `dataprocesses-node-development` skill. They are guidance, not a substitute for review or tests.

## Conduct and security

Participation is governed by [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md). Do not report vulnerabilities in public issues; follow [SECURITY.md](SECURITY.md).
