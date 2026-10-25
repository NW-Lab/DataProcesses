# StreamChartFS Block

Plots up to four channels from a Fast Stream frame on the Dashboard using sample-rate-aware time coordinates.

## Presentation

| Field | Value |
|---|---|
| Title | `StreamChartFS` |
| Subtitle | `Fast Stream Chart` |
| Icon | Reuses the StreamChartTS chart icon. |

## Dashboard

The Block creates a Dashboard image widget by default. It displays a five-second rolling line chart and honors the Dashboard FPS limit and auto-scroll / pause control.

| Setting | Default |
|---|---:|
| Show on Dashboard | `true` |
| Dashboard width | `4` grid cells |
| Dashboard height | `3` grid cells |

## Ports

| ID | Direction | Family | Required | Schema |
|---|---|---|---:|---|
| `input` | Input | Fast Stream | No | `FixedRateTimeSeries1D` (FS) (`FastStreamFrame`) with 1 to 4 channels. |

## Fast Stream input / processing

The frame's channel-major samples are plotted as separate traces. Each sample's time is computed from the frame start, `SampleRateHertz`, and sample index, preserving rates such as 44.1 kHz without accumulating rounded-period error. Frames without a positive `SampleRateHertz` are rejected (ADR 0005). The node retains at most 512 evenly sampled points per channel; the Dashboard history is trimmed to the visible five-second window. Frames with more than four channels or mismatched channel names/sample counts are rejected.