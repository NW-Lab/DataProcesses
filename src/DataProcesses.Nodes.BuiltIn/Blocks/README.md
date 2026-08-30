# Built-in Block layout

Human-facing Block behavior, common placed-Block settings, port appearance, and connection rules are defined in [../NodeBlockSpecification.md](../NodeBlockSpecification.md). Keep this layout guide focused on repository organization and per-Block ownership.

Each built-in Block has one self-contained directory under `Blocks/<BlockName>/`. A Block is one user-visible processing unit in the Flow Editor; its runtime implementation continues to use the public `INode` contract.

| File or directory | Required | Responsibility |
|---|---:|---|
| `<BlockName>Block.cs` | Yes | Stable type ID, port IDs, display metadata, category, and port contract. |
| `<BlockName>Node.cs` | Yes | Runtime processing logic; implement `INode`. |
| `<BlockName>NodeFactory.cs` | Yes | Create runtime instances and expose the Block definition. |
| `<BlockName>Settings.cs` | When needed | Immutable settings model and validation. |
| `icon.png` | Recommended | 64 x 64 PNG icon shown in the Node Library and on placed canvas Blocks. |
| `README.md` | Yes | Document port schema, settings, signal-processing, Payload, and interoperability behavior. See template below. |
| `Resources/` | When needed | Block-local localization keys or non-code assets. |

Mirror the same structure under `tests/DataProcesses.Nodes.BuiltIn.Tests/Blocks/<BlockName>/`. Keep tests deterministic and use synthetic data only.

## README Template

Create a Block-local `README.md` for every Block. For simple Blocks, keep sections concise; for complex Blocks, include full details for ports, schema, settings, timing, numerical behavior, and interoperability. Use [TestSignalTS/README.md](TestSignalTS/README.md) as the reference template.

```markdown
# <BlockName> Block

<One-sentence summary of what this Block does.>

## Presentation

| Field | Value |
|---|---|
| Title | `<TypeId short name>` |
| Subtitle | `<Optional short descriptor>` |
| Icon | `icon.png`, 64 x 64 PNG source rendered at 32 x 32 in the Node Library and 28 x 28 on the canvas. |

## Dashboard

<If this Block supports dashboard display, describe default settings and output format.>

## Ports

| ID | Direction | Family | Required | Schema |
|---|---|---|---:|---|
| `<port-id>` | Input/Output | Fast Stream/JSON Message | Yes/No | <Brief description of data schema>. |

## Settings

Block-specific settings use this JSON shape:

```json
{ ... }
```

| Field | Type | Default | Notes |
|---|---|---|---|
| `field` | type | value | Description. |

## Payload input / output

<If applicable, describe JsonMessage envelope structure and field semantics.>

## Fast Stream output / processing

<If applicable, describe frame structure, schema, channel names, and timing semantics.>
```

## Initial catalog

`BuiltInNodePlugin` explicitly registers the initial catalog below. The entries are contract-level runtime implementations; they do not imply that a corresponding Flow Editor or Dashboard visual component is already available.

| Directory | Type ID | Current role |
|---|---|---|
| `CsvInput/` | `dataprocesses.input.csv` | CSV source that reads `millis,CH1...` records from file or COM input and emits per-channel Fast Stream outputs as `millis,value`. |
| `CameraInputImage/` | `dataprocesses.input.camera-image` | Local camera source that captures one RGB24 `ImageFrame` for a JSON `Trigger: true` message or Dashboard Capture action. |
| `MovieInputImage/` | `dataprocesses.input.movie-image` | Movie source that emits RGB24 `ImageFrame` values at a configured FPS while JSON `isPlay` is true. |
| `UVCameraInputImage/` | `dataprocesses.input.uv-camera-image` | UV camera source that emits RGB24 `ImageFrame` values while JSON `isPlay` is true. |
| `TestSignalTS/` | `dataprocesses.test-signal` | Configurable Test Signal source with Payload settings input, Fast Stream output, and Payload status output. |
| `Trigger/` | `dataprocesses.trigger` | Payload source that emits configured values on manual action, execution start, and periodic timing. |
| `LowPassFilter/` | `dataprocesses.filter.low-pass` | Stateful first-order Fast Stream smoothing processor. |
| `FastFourierTransform/` | `dataprocesses.analysis.fft` | Fast Stream to one-sided `SpectrumFrame` analysis processor. |
| `StremOutputTS/` | `dataprocesses.output.stream` | Debug-oriented Fast Stream output adapter with bounded downsampling. |
| `StreamOutputVector/` | `dataprocesses.output.numeric-vector` | Debug-oriented Fast Stream vector sink for `NumericVectorFrame` snapshots. |
| `StreamChartVector/` | `dataprocesses.output.vector-chart` | Debug-oriented Fast Stream vector sink that renders a color-mapped intensity chart over a millisecond time axis. |
| `StreamChartSt/` | `dataprocesses.output.stream-chart-st` | Interactive multi-channel Fast Stream time-series chart sink (up to 4 channels) with configurable time alignment and channel names. |
| `StreamOutputImage/` | `dataprocesses.output.image` | Debug-oriented Fast Stream image sink for `ImageFrame` previews. |
| `PythonOutput/` | `dataprocesses.output.python` | Typed Fast Stream/JSON boundary that emits deferred-delivery status; it does not launch Python yet. |
| `PayloadOutput/` | `dataprocesses.output.payload` | Debug-oriented Payload sink that appends timestamped entries for dashboard inspection. |
| `CsvOutput/` | `dataprocesses.output.csv` | Fast Stream CSV sink that writes configured spans and tagged channels. |

## Adding a Block

1. Create `Blocks/<BlockName>/` using a PascalCase name such as `LowPassFilter`, `FastFourierTransform`, or `PythonOutput`.
2. Define the stable `TypeId` and typed ports in `<BlockName>Block.cs` before writing processing logic.
3. Implement the runtime node and factory within the same directory.
4. Add the factory to `BuiltInNodePlugin` so the Block is discoverable.
5. Add mirrored tests, documentation, and localization resources as applicable.
6. Run the Release build and complete test suite.

Do not place implementation files directly under `DataProcesses.Nodes.BuiltIn`. That project root is reserved for the built-in plugin catalog and project-level composition.

