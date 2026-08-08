# Image Output Block

Receives Fast Stream image frames and stores a bounded preview snapshot for diagnostics.

## Presentation

| Field | Value |
|---|---|
| Title | `Image Output` |
| Subtitle | `Debug image` |
| Icon | `icon.png`, 64 x 64 PNG source rendered at 32 x 32 in the Node Library and 28 x 28 on the canvas. |

## Dashboard

Image Output is shown on the dashboard by default.

| Setting | Default |
|---|---:|
| Show on Dashboard | `true` |
| Dashboard width | `3` grid cells |
| Dashboard height | `2` grid cells |

## Ports

| ID | Direction | Family | Required | Schema |
|---|---|---|---:|---|
| `input` | Input | Fast Stream | Yes | `Image2D` (`ImageFrame`) |

Image Output is a sink Block and has no output ports.

## Settings

This Block currently has no block-specific settings.

## Data examples

Typical input is an `ImageFrame` with these fields:

```json
{
  "name": "camera-1",
  "width": 640,
  "height": 480,
  "pixelFormat": "Rgb24",
  "pixelsInterleaved": "byte[]"
}
```

Large image payloads are clipped to an internal preview byte limit for dashboard-safe diagnostics.
