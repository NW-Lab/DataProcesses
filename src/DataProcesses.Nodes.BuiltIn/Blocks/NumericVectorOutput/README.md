# Numeric Vector Output Block

Receives Fast Stream numeric vectors and stores the latest bounded snapshot for diagnostics.

## Presentation

| Field | Value |
|---|---|
| Title | `Numeric Vector Output` |
| Subtitle | `Debug vector` |
| Icon | `icon.png`, 64 x 64 PNG source rendered at 32 x 32 in the Node Library and 28 x 28 on the canvas. |

## Dashboard

Numeric Vector Output is shown on the dashboard by default.

| Setting | Default |
|---|---:|
| Show on Dashboard | `true` |
| Dashboard width | `3` grid cells |
| Dashboard height | `2` grid cells |

## Ports

| ID | Direction | Family | Required | Schema |
|---|---|---|---:|---|
| `input` | Input | Fast Stream | Yes | `NumericVector1D` (`NumericVectorFrame`) |

Numeric Vector Output is a sink Block and has no output ports.

## Settings

This Block currently has no block-specific settings.

## Data examples

Typical input is a `NumericVectorFrame`:

```json
{
  "name": "fft-bin",
  "values": [0.12, 0.05, 0.01]
}
```

The preview stores at most 1024 values and down-samples larger vectors.
