# Test Signal Block

Generates a synthetic waveform as a Fast Stream frame and accepts Payload messages that update the signal settings while the Flow is running.

## Presentation

| Field | Value |
|---|---|
| Title | `TestSignal` |
| Subtitle | `Sin&squeare` |
| Icon | `icon.png`, 64 x 64 PNG source rendered at 32 x 32 in the Node Library and 28 x 28 on the canvas. |

## Dashboard

Test Signal is shown on the dashboard by default.

| Setting | Default |
|---|---:|
| Show on Dashboard | `true` |
| Dashboard width | `2` grid cells |
| Dashboard height | `1` grid cell |

The dashboard widget title follows the placed Block Name. If the placed Block is disabled, the widget title background is gray. The widget uses `contentKind: "text"` and displays the most recent Fast Stream output as `millis,value` rows for the first channel. `millis` is relative to the current Flow Editor run start. While the Flow Editor is in Run mode, the dashboard content is refreshed from repeated Test Signal output frames. Future Blocks may use graph content kinds such as time-series or X-Y displays instead of text.

## Ports

| ID | Direction | Family | Required | Schema |
|---|---|---|---:|---|
| `payload-in` | Input | Payload / JSON Message | No | Settings command payload with `isEnabled`, `waveType`, `frequency`, `samplePeriodMillis`, and `amplitude`. |
| `stream` | Output | Fast Stream | Yes | One-channel time-series frame named `signal`. The latest output is summarized on the dashboard as `millis,value` text when dashboard display is enabled. |
| `payload-out` | Output | Payload / JSON Message | No | Pass-through Payload messages and status payloads with `enabled`. |

## Settings

The placed Block has the common settings `name`, `description`, and `enabled`. When the common `enabled` setting is `false`, the runtime does not create or execute this Block.

Block-specific settings use this JSON shape:

```json
{
  "isEnabled": true,
  "waveType": "sine",
  "frequency": 10.0,
  "samplePeriodMillis": 1.0,
  "amplitude": 1.0,
  "payloadThrough": true
}
```

| Field | Type | Default | Notes |
|---|---|---|---|
| `isEnabled` | boolean | `true` | Controls whether this Block emits the Fast Stream frame when started. |
| `waveType` | string | `sine` | Accepts `sine` or `square`. |
| `frequency` | number | `10.0` | Frequency in hertz. Must be finite and greater than zero. |
| `samplePeriodMillis` | number | `1.0` | Sampling time in milliseconds. Must be finite and greater than zero. |
| `amplitude` | number | `1.0` | Peak amplitude. Must be finite and zero or greater. |
| `payloadThrough` | boolean | `true` | When true, incoming Payload messages are emitted unchanged on `payload-out`. |

## Payload input

The `payload-in` port accepts a `JsonMessage`. The message envelope remains the public `JsonMessage` contract. The `payload` object may contain any subset of the Block-specific settings.

```json
{
  "topic": "dataprocesses.test-signal.configure",
  "payload": {
    "isEnabled": true,
    "waveType": "square",
    "frequency": 5.0,
    "samplePeriodMillis": 2.0,
    "amplitude": 2.0
  },
  "timestamp": "2026-07-18T00:00:00Z"
}
```

Unknown payload fields are ignored. Invalid field types or invalid numeric ranges fail the packet handling call with an argument exception.

By default, Test Signal passes the incoming Payload message through unchanged to `payload-out`. Set `payloadThrough` to `false` in Block-specific settings to disable pass-through for this Block instance.

## Payload output

The `payload-out` port emits incoming Payload messages unchanged when `payloadThrough` is `true`. It also emits the enabled state when the Block starts and whenever `payload.isEnabled` changes.

```json
{
  "topic": "dataprocesses.test-signal.status",
  "payload": {
    "enabled": true
  },
  "timestamp": "2026-07-18T00:00:00Z"
}
```

## Fast Stream output

The `stream` port emits one frame on start when `isEnabled` is `true`. During Flow Editor Run mode, the flow is executed repeatedly so the dashboard receives fresh frames until execution is stopped.

| Field | Value |
|---|---|
| Sample period | `samplePeriodMillis * 1,000,000` ns. Default is 1,000,000 ns. |
| Sample count | 256 |
| Channels | One channel named `signal` |
| Sequence number | `0` |

For sine waves, sample `i` is `amplitude * sin(2 * pi * frequency * (frameStartSeconds + i * samplePeriodMillis / 1000))`. `frameStartSeconds` is based on the frame start timestamp, so repeated frames continue the waveform phase over time. For square waves, the output is `amplitude` when the sine phase is non-negative and `-amplitude` otherwise.

## Tests

Maintain tests for the stable port IDs, default sine output, settings parsing, configured factory creation, Payload commands, Payload status output, and disabled signal emission.