# FFTst Block

Converts a one-dimensional Fast Stream frame into a dashboard-friendly FFT magnitude vector.

## Presentation

| Field | Value |
|---|---|
| Title | `FFTst` |
| Subtitle | `Dashboard spectrum vector` |
| Icon | `icon.png`, 64 x 64 PNG source rendered at 32 x 32 in the Node Library and 28 x 28 on the canvas. |

## Dashboard

FFTst is shown on the dashboard by default as an equalizer-style magnitude bar image. It also emits `NumericVectorFrame` values so the result can be connected to `StreamChartVector` for a scrolling vector-chart dashboard display. Each vector index is one frequency bin; bin 0 is DC and subsequent bins advance by `sampleRate / sampleCount` hertz.

| Setting | Default |
|---|---:|
| Show on Dashboard | `true` |
| Dashboard width | `3` grid cells |
| Dashboard height | `2` grid cells |

## Ports

| ID | Direction | Family | Required | Schema |
|---|---|---|---:|---|
| `stream-in` | Input | Fast Stream | Yes | `TimeSeries1D` (`FastStreamFrame`) |
| `fft-vector` | Output | Fast Stream | Yes | `NumericVector1D` (`NumericVectorFrame`) |

## Settings

FFTst has no Block-specific settings in v0.1. It uses the whole incoming frame as the analysis window.

## Fast Stream processing

FFTst reads the first channel of each input frame and emits a one-sided magnitude vector named `fft-magnitude`. The output keeps the input sequence number and uses the input frame start time as the vector timestamp. Empty input frames emit an empty vector. Frames must have a positive sample period and consistent channel lengths.