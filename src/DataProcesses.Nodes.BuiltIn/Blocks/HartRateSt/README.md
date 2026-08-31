# HartRateSt Block

Detects heart-beat peaks from a Fast Stream time-series signal and emits a Fast Stream BPM series.

## Presentation

| Field | Value |
|---|---|
| Title | `HartRateSt` |
| Subtitle | `Heart rate` |
| Icon | `icon.png`, 64 x 64 PNG source rendered at 32 x 32 in the Node Library and 28 x 28 on the canvas. |

## Dashboard

HartRateSt is not shown on the dashboard by default. Connect `heart-rate` to a StreamChartSt or compatible Fast Stream output Block to inspect the detected BPM series.

## Ports

| ID | Direction | Family | Required | Schema |
|---|---|---|---:|---|
| `stream-in` | Input | Fast Stream | Yes | Time-series signal. The first channel is used for peak detection. |
| `heart-rate` | Output | Fast Stream | Yes | One-channel time series named `heart-rate-bpm`, containing BPM estimates. |

## Settings

HartRateSt currently has no Block-specific settings. Detection uses a local maximum above 60% of the current frame amplitude range, a 300 ms refractory interval, and a 2,000 ms maximum interval for accepted BPM estimates.

## Fast Stream output / processing

The output frame preserves the input frame start time, sample period, and sequence number. The output has the same sample count as the first input channel. Values are `NaN` until two valid heart-beat peaks have been accepted. Once an interval is accepted, subsequent samples carry the latest BPM estimate until a newer estimate is detected.

Non-finite input samples break the local peak window. Multi-channel input is accepted, but only the first channel is analyzed in this initial Block contract.