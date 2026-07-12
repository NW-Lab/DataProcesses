# ADR 0001: Initial platform and two data paths

**Status:** Accepted for the v0.1 scaffold  
**Date:** 2026-07-12

## Context

DataProcesses must deliver a Windows desktop application first without creating an unnecessary barrier to later macOS support. The visual graph will process both high-rate numeric samples and lower-rate events or commands. Treating both workloads as text messages would simplify the surface API but would add avoidable parsing and allocation to the sample-processing path.

## Decision

The desktop UI uses **Avalonia UI** on **C#/.NET**. Domain logic and plugin contracts remain independent of Avalonia.

Block connections expose two visible port families:

| Family | Internal representation | Visual convention |
|---|---|---|
| Fast Stream | Typed frame containing numeric channel buffers and timing metadata | Blue circle, `S`, solid line |
| JSON Message | Versioned envelope containing a JSON `payload` | Orange diamond, `J`, dashed line |

CSV, including `millis,data`, is an import, export, storage, and explicit interoperability format. It is not the internal Fast Stream representation. Detailed schemas such as time series and spectrum remain distinguishable even when they share the Fast Stream visual family.

Built-in and third-party nodes implement contracts from `DataProcesses.Plugin.Abstractions`. Initial managed plugins run in process and are therefore trusted code. A future worker protocol will support Python and stronger failure isolation out of process.

## Consequences

Windows is the first supported packaging target, while UI code starts from a cross-platform framework. Fast Stream implementations must avoid per-sample JSON or CSV conversion. Connection validation must check direction, port family, and detailed schema. Conversion between Fast Stream and JSON Message requires an explicit conversion node.

The public plugin contracts require conservative versioning. Breaking changes before v1.0 remain possible, but every such change must be documented and tested.
