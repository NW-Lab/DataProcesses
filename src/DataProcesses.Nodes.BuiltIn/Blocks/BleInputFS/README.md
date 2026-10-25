# BleInputFS Block

Receives CSV or headerless binary BLE GATT notifications and emits regularly sampled, multi-channel Fast Stream frames.

## Presentation and Dashboard

Title: `BleInputFS`; subtitle: `BLE GATT`; reuses the BLE Input TS icon.
Hidden on the dashboard by default. Connect `stream` to StreamOutputFS, StreamChartFS, FilterFS, or another compatible node.

## Ports

| ID | Direction | Family | Required | Schema |
|---|---|---|---|---|
| `stream` | Output | Fast Stream | No | `FixedRateTimeSeries1D` (FS), channel-major double buffers named `data1` through `dataN`. |

## Settings

The Inspector persists these settings in the placed Block's `settingsJson`. Changes take effect on the next run.

```json
{
  "deviceId": "",
  "deviceName": "",
  "autoConnect": true,
  "serviceUuid": "6e400001-b5a3-f393-e0a9-e50e24dcca9e",
  "notifyCharacteristicUuid": "6e400003-b5a3-f393-e0a9-e50e24dcca9e",
  "channelCount": 2,
  "timeoutMilliseconds": 5000,
  "sampleRateHertz": 1000,
  "format": "Csv",
  "numericType": "Float32",
  "byteOrder": "LittleEndian"
}
```

| Setting | Range / behavior |
|---|---|
| Device, service and characteristic | Uses the existing BLE Inspector scan, selection, persisted identifier and Nordic UART UUID reset. |
| `autoConnect` | Retained host reconnect preference, matching BleInputTS; starting the node explicitly connects to the selected device. |
| `channelCount` | 1-16. |
| `timeoutMilliseconds` | 1-600000; inactivity while awaiting a notification raises `TimeoutException`, not successful end-of-stream. |
| `sampleRateHertz` | Integer 1-1000000 Hz; must match the sender. This is metadata, not a command configuring the peripheral. BLE throughput limits still apply. |
| `format` | `Csv` or `Binary`. |
| `numericType` | `Int16`, `Int32`, `Float32` or `Float64`; signed integers or IEEE-754 floating point, converted to double without scaling. Binary only. |
| `byteOrder` | `LittleEndian` or `BigEndian`. Binary only. |

Binary controls are visible only when Binary is selected; switching back to CSV retains their settings.

## External input formats

CSV uses ASCII-compatible UTF-8 numeric rows `millis,data1,...,dataN`, invariant-culture decimals and LF or CRLF endings, without a header. Empty lines are ignored. Lines may span notifications, but must end in LF and be at most 4096 bytes before LF. Values must be finite; `millis` is nonnegative relative device time and must strictly increase at nanosecond precision.

The first CSV sample anchors to host receipt time. Subsequent timestamps retain device-relative offsets. Rows matching the configured sampling grid (within one nanosecond rounding tolerance) share a frame; a time discontinuity starts a new frame. No interpolation, resampling, or missing-value insertion occurs. Use decimal milliseconds for fractional-millisecond periods. A sender with coarse or irregular timestamps may produce many small frames.

Binary uses no header or timestamps: `[sample0 CH1][sample0 CH2]...[sample1 CH1]...`. Notifications may split a number or channel tuple. The first complete sample anchors to host receipt time, and all subsequent timing derives from the absolute sample index at the configured rate. Missing samples cannot be detected without sender framing. Nonfinite floats are rejected. An incomplete line or tuple at normal end-of-stream raises `InvalidDataException`.

CSV exists only at this explicit device boundary; output between nodes is always typed Fast Stream, never CSV or JSON.

## Timing, ownership and lifecycle

`SampleRateHertz` is authoritative; `SamplePeriodNanoseconds` is a nominal integer fallback. Frames contain up to 256 samples per channel and flush at notification boundaries and CSV timing discontinuities. Rational-rate frame origins use absolute sample indices, avoiding accumulated period rounding; within-frame timestamps may differ by at most one nanosecond from the global rational grid after a frame boundary.

Output arrays are owned by each frame and are never reused or mutated after emission. Binary decoding uses a fixed partial-tuple buffer and frame-level allocations, not per-sample objects. Emission is awaited before reading another notification. The shared Windows GATT adapter currently queues notifications in an unbounded channel, so this is not end-to-end peripheral backpressure.

Every run resets sequence numbers, partial input and timing. Concurrent starts are rejected. Cancellation and Stop cancel the source and dispose its subscription; partial input is discarded on cancellation. Source, parsing and emission failures propagate. Physical BLE access currently requires the Windows target; non-Windows execution reports `PlatformNotSupportedException`. No public Add-in contract or existing BleInputTS setting is changed.

## Validation

Synthetic tests cover every numeric type / byte order, fragmented notifications, timing gaps, exact rates, malformed and truncated input, settings, stop, cancellation, timeout, restart and registration. `Binary_RecordsThroughputAndFrameLevelAllocations` measures elapsed time and allocation for 131072 two-channel samples and checks a 64-byte/sample allocation ceiling (including both channels; transport excluded).

```powershell
dotnet test tests\DataProcesses.Nodes.BuiltIn.Tests\DataProcesses.Nodes.BuiltIn.Tests.csproj --configuration Release --filter FullyQualifiedName~BleInputFS --logger "console;verbosity=detailed"
```

Manual UI/hardware check: add BleInputFS from the INPUT library, select CSV then Binary in the Inspector, verify the binary controls appear and are keyboard accessible, change all settings, save/reload the Flow, and scan/select a synthetic-data BLE sender. Connect StreamOutputFS and verify the configured rate and values, then stop and confirm notification unsubscribe. Repeat with Japanese/English UI cultures, light/dark themes and display scaling. Automated tests do not substitute for this physical BLE check.
