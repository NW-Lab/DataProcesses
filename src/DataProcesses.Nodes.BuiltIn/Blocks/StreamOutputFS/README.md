# StreamOutputFS Block

Receives a generic Fast Stream time series and keeps a bounded latest snapshot with its sample frequency.

## Presentation

| Field | Value |
|---|---|
| Title | `StreamOutputFS` |
| Subtitle | `Fast Stream output` |
| Icon | Reuses the Stream Output icon. |

## Dashboard
The Dashboard shows the sample frequency in hertz and a line of values for each channel. Values are evenly sampled to match the app-wide Dashboard FPS configured in the Dashboard toolbar, with a maximum of 500 values per channel. Dashboard FPS defaults to 10 and does not change acquisition or flow processing rates. The frequency comes from `SampleRateHertz` when provided, or is derived from the legacy `SamplePeriodNanoseconds` value.

## Ports

| ID | Direction | Family | Required | Schema |
|---|---|---|---:|---|
| `input` | Input | Fast Stream | Yes | `TimeSeries1D` (`FastStreamFrame`) |

## Settings

This Block currently has no Block-specific settings.

## Fast Stream output / processing

This is a sink Block with no output ports. Its latest snapshot retains the sample rate, channel names, sequence number, and at most 512 samples per channel for diagnostics.