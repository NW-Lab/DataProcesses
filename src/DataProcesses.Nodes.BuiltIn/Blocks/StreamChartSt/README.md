# StreamChartSt Block

Renders multi-channel Fast Stream time-series data as an interactive time-series chart (XY recorder style). Up to 4 Fast Stream inputs are supported.

## Presentation

| Field | Value |
|---|---|
| Title | `StreamChartSt` |
| Subtitle | `Time-series Chart` |
| Icon | `icon.png`, 64 x 64 PNG source rendered at 32 x 32 in the Node Library and 28 x 28 on the canvas. |

## Dashboard

StreamChartSt is shown on the dashboard by default with time-series line chart rendering and auto-scroll / pause control.

| Setting | Default |
|---|---:|
| Show on Dashboard | `true` |
| Dashboard width | `3` grid cells |
| Dashboard height | `3` grid cells |

## Ports

| ID | Direction | Family | Required | Schema |
|---|---|---|---:|---|
| `stream-1` | Input | Fast Stream | No | `TimeSeries1D` (`FastStreamFrame`) |
| `stream-2` | Input | Fast Stream | No | `TimeSeries1D` (`FastStreamFrame`) |
| `stream-3` | Input | Fast Stream | No | `TimeSeries1D` (`FastStreamFrame`) |
| `stream-4` | Input | Fast Stream | No | `TimeSeries1D` (`FastStreamFrame`) |

## Settings

| Key | Type | Default | Description |
|---|---|---|---|
| `timeAlignmentMode` | string | `independent` | Time axis alignment across channels: `independent` or `alignToFirstStream`. |
| `timeSpanMillis` | number | `5000` | Visible horizontal time window in milliseconds (default 5000ms / 5s). |
| `channel1Name` | string | `CH1` | Custom display label for Channel 1. |
| `channel2Name` | string | `CH2` | Custom display label for Channel 2. |
| `channel3Name` | string | `CH3` | Custom display label for Channel 3. |
| `channel4Name` | string | `CH4` | Custom display label for Channel 4. |

```json
{
  "timeAlignmentMode": "independent",
  "timeSpanMillis": 5000,
  "channel1Name": "CH1",
  "channel2Name": "CH2",
  "channel3Name": "CH3",
  "channel4Name": "CH4"
}
```

## Time alignment and channels

- `independent`: Each channel's elapsed milliseconds are measured relative to its own first received frame timestamp.
- `alignToFirstStream`: All channel time offsets are measured relative to the first stream's (`stream-1`) starting timestamp.
