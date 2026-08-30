# StreamChartVector Block

Renders Fast Stream numeric vectors as a time-series intensity chart. The horizontal axis is
millisecond time, the vertical axis is the vector index (index zero at the bottom), and color
encodes the sample value.

## Presentation

| Field | Value |
|---|---|
| Title | `StreamChartVector` |
| Subtitle | `Vector waterfall chart` |
| Icon | `icon.png`, 64 x 64 PNG source rendered at 32 x 32 in the Node Library and 28 x 28 on the canvas. |

## Dashboard

StreamChartVector is shown on the dashboard by default and provides the shared Pause button,
which freezes the chart without stopping the flow.

| Setting | Default |
|---|---:|
| Show on Dashboard | `true` |
| Dashboard width | `6` grid cells |
| Dashboard height | `4` grid cells |

## Ports

| ID | Direction | Family | Required | Schema |
|---|---|---|---:|---|
| `input` | Input | Fast Stream | Yes | `NumericVector1D` (`NumericVectorFrame`) |

StreamChartVector accepts exactly one Numeric Vector input and has no output ports.

## Settings

| Key | Type | Default | Description |
|---|---|---|---|
| `colorMap` | string | `jet` | One of `jet`, `grayscale`, `hot`, `viridis`. |
| `autoScale` | boolean | `true` | Derive the intensity range from the visible window. |
| `minValue` | number | `0` | Lower intensity bound when `autoScale` is `false`. |
| `maxValue` | number | `1` | Upper intensity bound when `autoScale` is `false`. |
| `interpolate` | boolean | `true` | Blend between adjacent samples instead of holding the previous value. |
| `timeSpanMillis` | number | `5000` | Visible time window in milliseconds (100 to 600000). |

```json
{
  "colorMap": "jet",
  "autoScale": true,
  "minValue": 0,
  "maxValue": 1,
  "interpolate": true,
  "timeSpanMillis": 5000
}
```

## Time axis

The millisecond position of each column comes from `NumericVectorFrame.Timestamp`, measured
relative to the first frame received after the flow starts. Frames without a timestamp fall back
to `SequenceNumber` as the millisecond position. Samples older than `timeSpanMillis` are dropped,
except for one sample retained just outside the window so the leftmost pixel column stays filled.

Vectors longer than 512 elements are down-sampled to 512 rows.
