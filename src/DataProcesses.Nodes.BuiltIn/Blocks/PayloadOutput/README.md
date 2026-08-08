# Payload Output Block

Receives JSON Message payloads and records timestamped debug entries.

## Presentation

| Field | Value |
|---|---|
| Title | `Payload Output` |
| Subtitle | `Debug payload` |
| Icon | `icon.png`, 64 x 64 PNG source rendered at 32 x 32 in the Node Library and 28 x 28 on the canvas. |

## Dashboard

Payload Output is shown on the dashboard by default.

| Setting | Default |
|---|---:|
| Show on Dashboard | `true` |
| Dashboard width | `2` grid cells |
| Dashboard height | `1` grid cell |

## Ports

| ID | Direction | Family | Required | Schema |
|---|---|---|---:|---|
| `payload` | Input | Payload / JSON Message | Yes | `JsonEnvelope` (`JsonMessage`) |

Payload Output is a sink Block and has no output ports.

## Settings

This Block currently has no block-specific settings.

## Data examples

Typical input envelope:

```json
{
  "topic": "sensor/status",
  "payload": {
    "ok": true
  },
  "timestamp": "2026-07-18T00:00:00Z",
  "correlationId": "optional-id"
}
```

The Block writes a formatted debug log entry per message.
