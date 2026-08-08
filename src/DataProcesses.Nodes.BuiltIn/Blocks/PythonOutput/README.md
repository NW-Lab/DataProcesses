# Python Output Block

Bridges Fast Stream and Payload inputs to a deferred status output without launching Python yet.

## Presentation

| Field | Value |
|---|---|
| Title | `Python Output` |
| Subtitle | `Deferred bridge` |
| Icon | `icon.png`, 64 x 64 PNG source rendered at 32 x 32 in the Node Library and 28 x 28 on the canvas. |

## Ports

| ID | Direction | Family | Required | Schema |
|---|---|---|---:|---|
| `fast-stream` | Input | Fast Stream | No | `Unspecified` (family-level compatibility) |
| `message` | Input | Payload / JSON Message | No | `JsonEnvelope` (`JsonMessage`) |
| `status` | Output | Payload / JSON Message | No | `JsonEnvelope` (`JsonMessage`) |

## Settings

This Block currently has no block-specific settings.

## Data examples

When a packet is received on either input, the Block can emit a status message such as:

```json
{
  "topic": "dataprocesses.python-output.received",
  "payload": {
    "sourcePortId": "message",
    "kind": "JsonMessage"
  },
  "timestamp": "2026-07-18T00:00:00Z"
}
```
