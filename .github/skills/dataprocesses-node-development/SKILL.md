---
name: dataprocesses-node-development
description: Design, implement, test, or review DataProcesses processing nodes and their typed ports. Use when adding built-in or plugin nodes such as signal generators, filters, FFT processors, converters, Python outputs, or dashboard sinks; when changing node contracts; or when diagnosing node compatibility and Fast Stream performance.
---

# DataProcesses node development

Build nodes through the public abstractions while preserving the Fast Stream and JSON Message separation.

## Prepare

1. Read `docs/initial-specification.md`, the accepted ADRs, `.github/copilot-instructions.md`, and the nearest path-specific instructions.
2. Inspect `src/DataProcesses.Plugin.Abstractions`, one similar built-in node, and its tests before editing.
3. State the node responsibility in one sentence. Split unrelated responsibilities into separate nodes.
4. Read [references/node-contract-checklist.md](references/node-contract-checklist.md) when defining ports, settings, timing, or acceptance tests.

## Select the data family

Use **Fast Stream** for high-rate numeric time series or spectra. Process typed frames and buffers; do not serialize frames to CSV or JSON inside the graph.

Use **JSON Message** for events, commands, state changes, metadata, and variable payloads. Define a versioned envelope and validate the payload schema at the receiving boundary.

Require an explicit conversion node when crossing families. Reject incompatible connections during graph validation rather than relying on runtime casts.

## Define the contract first

Specify stable node and port identifiers, display resource keys, port directions, data family, detailed schema, units, channel behavior, timing semantics, settings, validation rules, cancellation behavior, error behavior, and statefulness.

Treat identifiers and persistence-facing names as compatibility commitments. Do not expose Avalonia, serializer-specific, or container-specific types through the plugin API. Propose an ADR before making a breaking public-contract or project-schema change.

## Implement the node

Keep processing independent of the UI. Make configuration immutable while a run is active. Validate configuration before execution. Pass cancellation through execution boundaries and dispose owned resources deterministically.

For Fast Stream, process frames or spans, make buffer ownership explicit, preserve timing metadata, and avoid per-sample allocation, boxing, LINQ, or copying in measured hot paths. For stateful DSP, define reset behavior and how discontinuities, sample-rate changes, empty frames, and invalid numeric values are handled.

For JSON Message, validate expected fields and version, produce actionable errors, and avoid embedding large sample arrays.

## Test before integrating

Add deterministic tests using synthetic data. Cover metadata, port declarations, valid output, invalid configuration, empty input, cancellation, and node-specific edge cases. For filters and FFT, compare against known signals and tolerances. For material hot-path changes, add a benchmark or repeatable measurement and report allocations as well as elapsed time.

Run:

```bash
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
```

## Finish

Update user-facing documentation, localization resources, examples, and an ADR when the change alters architecture or compatibility. Summarize the contract, implementation, tests, measured performance where relevant, and remaining risks. Do not claim completion when build or tests fail.
