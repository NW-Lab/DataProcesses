# Built-in Block layout

Human-facing Block behavior, common placed-Block settings, port appearance, and connection rules are defined in [../NodeBlockSpecification.md](../NodeBlockSpecification.md). Keep this layout guide focused on repository organization and per-Block ownership.

Each built-in Block has one self-contained directory under `Blocks/<BlockName>/`. A Block is one user-visible processing unit in the Flow Editor; its runtime implementation continues to use the public `INode` contract.

| File or directory | Required | Responsibility |
|---|---:|---|
| `<BlockName>Block.cs` | Yes | Stable type ID, port IDs, display metadata, category, and port contract. |
| `<BlockName>Node.cs` | Yes | Runtime processing logic; implement `INode`. |
| `<BlockName>NodeFactory.cs` | Yes | Create runtime instances and expose the Block definition. |
| `<BlockName>Settings.cs` | When needed | Immutable settings model and validation. |
| `Resources/` | When needed | Block-local localization keys or non-code assets. |
| `icon.png` | When needed | 64 x 64 PNG icon shown in the Node Library and on placed canvas Blocks. |
| `README.md` | When needed | Explain port schema, settings, signal-processing, Payload, or interoperability behavior. |

Mirror the same structure under `tests/DataProcesses.Nodes.BuiltIn.Tests/Blocks/<BlockName>/`. Keep tests deterministic and use synthetic data only.

Create a Block-local `README.md` whenever the Block defines non-trivial settings, multiple ports, Payload fields, Payload pass-through behavior, timing semantics, numerical behavior, icon behavior, or interoperability behavior. Treat `TestSignal/README.md` as the initial template for future Block specifications.

## Initial catalog

`BuiltInNodePlugin` explicitly registers the initial catalog below. The entries are contract-level runtime implementations; they do not imply that a corresponding Flow Editor or Dashboard visual component is already available.

| Directory | Type ID | Current role |
|---|---|---|
| `TestSignal/` | `dataprocesses.test-signal` | Configurable Test Signal source with Payload settings input, Fast Stream output, and Payload status output. |
| `LowPassFilter/` | `dataprocesses.filter.low-pass` | Stateful first-order Fast Stream smoothing processor. |
| `FastFourierTransform/` | `dataprocesses.analysis.fft` | Fast Stream to one-sided `SpectrumFrame` analysis processor. |
| `TimeSeriesDisplay/` | `dataprocesses.dashboard.time-series` | Fast Stream display-state adapter with bounded downsampling. |
| `PythonOutput/` | `dataprocesses.output.python` | Typed Fast Stream/JSON boundary that emits deferred-delivery status; it does not launch Python yet. |

## Adding a Block

1. Create `Blocks/<BlockName>/` using a PascalCase name such as `LowPassFilter`, `FastFourierTransform`, or `PythonOutput`.
2. Define the stable `TypeId` and typed ports in `<BlockName>Block.cs` before writing processing logic.
3. Implement the runtime node and factory within the same directory.
4. Add the factory to `BuiltInNodePlugin` so the Block is discoverable.
5. Add mirrored tests, documentation, and localization resources as applicable.
6. Run the Release build and complete test suite.

Do not place implementation files directly under `DataProcesses.Nodes.BuiltIn`. That project root is reserved for the built-in plugin catalog and project-level composition.
