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

For time-series Blocks, use the `TS` suffix for `TimeSeries1D` (timestamp or nominal-period timing, such as Arduino `millis,data` input) and the `FS` suffix for `FixedRateTimeSeries1D` (exact `SampleRateHertz`, such as 44.1 kHz audio). TS and FS ports do not connect to each other; see ADR 0005. Keep existing type IDs such as `dataprocesses.filter.filter-st` unchanged when renaming. If a persisted type ID itself must change, such as the spelling fix from `hart-rate` to `heart-rate`, keep the old ID as a `LegacyTypeId` constant and register it in `BuiltInNodePlugin.LegacyTypeIdAliases`; `ProjectFileService` rewrites aliased IDs when a project is loaded, and the next save persists the current ID.

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
| `SerialInputTS/` | `dataprocesses.input.serial-st` | Arduino USB serial source that reads `millis,data1,data2,...` rows and emits one multi-channel Fast Stream output. |
| `SerialInputVector/` | `dataprocesses.input.serial-vector` | Arduino USB serial source that reads contemporaneous IMU `millis,x,y,z` rows and emits one numeric vector per row. |
| `BleInputTS/` | `dataprocesses.input.ble-st` | Arduino BLE GATT source that receives Nordic UART CSV notifications and emits one multi-channel Fast Stream output. |
| `BleInputFS/` | `dataprocesses.input.ble-fs` | BLE GATT source with Inspector-selectable CSV or binary input, numeric type, byte order and exact sample rate. |
| `BleInputVector/` | `dataprocesses.input.ble-vector` | Arduino BLE GATT source that receives Nordic UART `millis,x,y,z` IMU notifications and emits one timestamped numeric vector per row. |
| `CameraInputImage/` | `dataprocesses.input.camera-image` | Local camera source that captures one RGB24 `ImageFrame` for a JSON `Trigger: true` message or Dashboard Capture action. |
| `MovieInputImage/` | `dataprocesses.input.movie-image` | Movie source that emits RGB24 `ImageFrame` values at a configured FPS while JSON `isPlay` is true. |
| `UVCameraInputImage/` | `dataprocesses.input.uv-camera-image` | UV camera source that emits RGB24 `ImageFrame` values while JSON `isPlay` is true. |
| `MicInputFS/` | `dataprocesses.input.mic-fs` | Windows default microphone source with configurable sample rate and Stereo/Left/Right Fast Stream output. |
| `AudioOutFS/` | `dataprocesses.output.audio-fs` | WASAPI Fast Stream audio playback with Dashboard mute/record controls and Inspector-selected WAV folder/prefix. |
| `MqttIn/` | `dataprocesses.input.mqtt-in` | MQTT subscription source that parses broker payloads as JSON and emits `JsonMessage` packets. |
| `MqttOut/` | `dataprocesses.output.mqtt-out` | Publishes JSON Message payloads to an MQTT broker with configurable topic, QoS, and retain behavior. |
| `TestSignalTS/` | `dataprocesses.test-signal` | Configurable Test Signal source with Payload settings input, Fast Stream output, and Payload status output. |
| `TestSignalFS/` | `dataprocesses.test-signal-fs` | Sine or square Fast Stream source with configurable peak intensity and exact sample rate. |
| `Trigger/` | `dataprocesses.trigger` | Payload source that emits configured values on manual action, execution start, and periodic timing. |
| `FilterTS/` | `dataprocesses.filter.filter-st` | `FilterTS`: configurable stateful Fast Stream low-pass, high-pass, band-pass, and band-stop processor. |
| `FilterFS/` | `dataprocesses.filter.filter-fs` | Fast Stream low-pass, high-pass, band-pass, band-stop, or notch processor with Inspector-configurable frequency and order. |
| `MovingAverage/` | `dataprocesses.filter.moving-average` | Stateful Fast Stream moving average with a sample-count or elapsed-time window. |
| `FastFourierTransform/` | `dataprocesses.analysis.fft` | Fast Stream to one-sided `SpectrumFrame` analysis processor. |
| `FftFS/` | `dataprocesses.analysis.fft-fs` | Fast Stream FFT entry point preserving exact integer sample-rate metadata in the spectrum. |
| `FftTS/` | `dataprocesses.analysis.fft-st` | `FFT_TS`: Fast Stream to one-sided `NumericVectorFrame` spectrum processor for dashboard vector-chart display. |
| `CdTimeResolvedMethodTS/` | `dataprocesses.analysis.cd-time-resolved-method-st` | Fast Stream central-difference processor that emits a time-resolved numeric vector. |
| `HeartRateTS/` | `dataprocesses.analysis.heart-rate-st` (legacy alias `hart-rate-st`) | Fast Stream heart-beat peak detector that emits BPM as a one-channel time-series frame. |
| `HeartRateImage/` | `dataprocesses.analysis.heart-rate-image` (legacy alias `hart-rate-image`) | Image-frame rPPG processor that predicts BPM from central ROI color changes and emits a one-channel time-series frame. |
| `HumansImage/` | `dataprocesses.analysis.humans-image` | Image-frame face-region detector that emits a one-channel person-count time-series frame. |
| `BreathImage/` | `dataprocesses.analysis.breath-image` | Image-frame respiration estimator that predicts breaths per minute from central ROI Cg changes. |
| `BreathTS/` | `dataprocesses.analysis.breath-st` | Fast Stream respiration detector that emits breaths per minute and optional JSON anomaly events. |
| `StreamOutputTS/` | `dataprocesses.output.stream` | Debug-oriented Fast Stream output adapter with bounded downsampling. |
| `StreamOutputFS/` | `dataprocesses.output.stream-fs` | Fast Stream diagnostic sink that shows sample frequency and Dashboard-rate values. |
| `StreamOutputVector/` | `dataprocesses.output.numeric-vector` | Debug-oriented Fast Stream vector sink for `NumericVectorFrame` snapshots. |
| `StreamChartVector/` | `dataprocesses.output.vector-chart` | Debug-oriented Fast Stream vector sink that renders a color-mapped intensity chart over a millisecond time axis. |
| `StreamChartTS/` | `dataprocesses.output.stream-chart-st` | `StreamChartTS`: interactive multi-channel Fast Stream time-series chart sink (up to 4 channels) with configurable time alignment and channel names. |
| `StreamChartFS/` | `dataprocesses.output.stream-chart-fs` | Dashboard chart for up to four channels in one Fast Stream frame, using exact sample-rate timing when available. |
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
