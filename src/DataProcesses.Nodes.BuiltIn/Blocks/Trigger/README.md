# Trigger Block

Emits configured JSON Message payloads on startup, manual trigger, or periodic schedule.

## Presentation

| Field | Value |
|---|---|
| Title | `Trigger` |
| Subtitle | `Manual/start/periodic` |
| Icon | `icon.png`, 64 x 64 PNG source rendered at 32 x 32 in the Node Library and 28 x 28 on the canvas. |

## Dashboard

Trigger is shown on the dashboard by default.

| Setting | Default |
|---|---:|
| Show on Dashboard | `true` |
| Dashboard width | `2` grid cells |
| Dashboard height | `1` grid cell |

## Ports

Trigger is a source Block. It has no input ports.

| ID | Direction | Family | Required | Schema |
|---|---|---|---:|---|
| `payload-out` | Output | Payload / JSON Message | Yes | `JsonEnvelope` (`JsonMessage`) |

## Settings

Block-specific settings use this JSON shape:

```json
{
  "topic": "dataprocesses.trigger",
  "payloadPath": "payload.value",
  "payloadValueType": "DateTime",
  "boolValue": true,
  "stringValue": "trigger",
  "numberValue": 1.0,
  "numberArrayText": "1,2,3",
  "emitOnExecutionStart": true,
  "emitPeriodically": false,
  "initialDelayMilliseconds": 0,
  "repeatIntervalMilliseconds": 1000,
  "executionSessionId": 0,
  "manualTriggerNonce": 0
}
```

See `TriggerSettings` for full validation and enum values.

## Data examples

Typical emitted envelope:

```json
{
  "topic": "dataprocesses.trigger",
  "payload": {
    "value": 1.0
  },
  "timestamp": "2026-07-18T00:00:00Z"
}
```
