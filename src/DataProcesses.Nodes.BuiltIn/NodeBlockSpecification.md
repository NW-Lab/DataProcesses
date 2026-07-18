# Node Block specification

This document describes the user-facing contract for built-in Node Blocks. A Block is one processing unit placed on the Flow Editor canvas. Runtime code still implements the public `INode` contract, but product documentation and UI should use **Block** for the user-visible unit.

## Terms

| Term | Meaning |
|---|---|
| Block | User-visible processing unit placed on the Flow Editor canvas. |
| Node | Runtime implementation of a Block through `INode`. |
| Port | Typed input or output endpoint on a Block. |
| Fast Stream | High-rate numeric data path for time series and derived numeric frames. |
| Payload | User-facing name for the JSON Message data path. Internally this uses the existing `JsonMessage` contract. |

## Placed Block settings

Every Block instance placed on a Flow has common settings. Block implementations may define additional settings in their own directory.

| Setting | Type | Required | Behavior |
|---|---|---:|---|
| Name | string | Yes | Display name for this placed Block instance. Defaults to the Block definition display name. |
| Description | string | No | Free text note for the user. Does not affect runtime behavior. |
| Enabled | boolean | Yes | When `false`, the Block is not executed and emits no data. Connections remain in the Flow document. |

Block-specific settings are stored as JSON and are interpreted by the Block. Settings must use culture-invariant numbers and stable property names.

## Block presentation

Each Block definition may provide a title, subtitle, and icon path. The title and subtitle describe the Block type in the Node Library. They are definition metadata, not per-instance settings.

| Field | Display rule |
|---|---|
| Title | Shown in the Node Library. Used as the default placed-Block name when a Block is added to the canvas. |
| Subtitle | Shown only in the Node Library. Do not show it on placed canvas Blocks. |
| Icon | Shown in the Node Library and on placed canvas Blocks. |

Icon source files should be PNG images named `icon.png` in the Block directory. Use a 64 x 64 pixel source image. The Flow Editor should render it at 32 x 32 pixels in the Node Library and 28 x 28 pixels on placed canvas Blocks.

Placed canvas Blocks show the instance Name only. If Name is empty, the canvas falls back to the Block title.

## Ports

A Block may define multiple Fast Stream inputs, multiple Payload inputs, multiple Fast Stream outputs, and multiple Payload outputs. Each port has a stable ID, display name, direction, data family, required/optional flag, and a documented payload or frame schema.

Port IDs are stable compatibility identifiers. Do not reuse the same ID for different meanings. Prefer distinct IDs for input and output ports, such as `payload-in` and `payload-out`, even when both are Payload ports.

## Data families

### Fast Stream

Fast Stream is the internal high-throughput numeric path. The current public frame contract stores timing metadata once per frame and sample values as channel-major `double` buffers. This avoids per-sample JSON or CSV allocation in the processing graph.

The human and external interchange shape for simple time-series data may be described as:

```csv
millis,ch1,ch2,...
0,0.0,1.0
1,0.1,0.9
```

`millis` is relative to the frame or recording start unless a Block explicitly documents otherwise. This CSV-like shape is for import, export, examples, and user documentation; it is not the internal transport between Blocks.

### Payload

Payload is the user-facing name for the JSON Message path. Internally it uses the current `JsonMessage` envelope:

```json
{
  "topic": "block/status",
  "payload": {
    "enabled": true
  },
  "timestamp": "2026-07-18T00:00:00Z",
  "correlationId": "optional-request-id"
}
```

Block documentation must define the expected `payload` object fields, required fields, optional fields, accepted unknown-field behavior, and output topics.

By default, Blocks that accept Payload input should be able to pass the incoming `JsonMessage` through to a Payload output when that behavior is appropriate for the Block. This must be configurable per Block, either in code or in Block settings. A Block README must state whether Payload pass-through is enabled and how to change it.

## Visual rules

Fast Stream and Payload must be distinguishable without relying only on color.

| Family | Port color | Port shape | Label | Connection line |
|---|---|---|---|---|
| Fast Stream | Blue | Circle | `S` | Solid |
| Payload | Red | Circle | `P` | Dashed |

Tooltips should include the port name, direction, data family, and schema summary when available.

## Connection rules

Connections are directed from an output port to an input port. Fast Stream ports can connect only to Fast Stream ports. Payload ports can connect only to Payload ports.

One output may fan out to multiple input ports on multiple downstream Blocks. The runtime delivers each emitted packet to every matching outgoing connection.

The MVP Flow remains a directed acyclic graph. Cycles are rejected during validation.

## Block-local documentation

Create a `README.md` in a Block directory when the Block has any non-trivial port schema, settings, timing behavior, numerical behavior, or Payload contract. The README should include:

1. Responsibility in one sentence.
2. Port table with IDs, directions, data families, required flags, and schemas.
3. Common and Block-specific settings.
4. Runtime behavior and lifecycle notes.
5. Payload input and output examples when applicable.
6. Fast Stream timing, units, and channel behavior when applicable.
7. Validation and test expectations.

See `Blocks/TestSignal/README.md` for the first template-style Block specification.