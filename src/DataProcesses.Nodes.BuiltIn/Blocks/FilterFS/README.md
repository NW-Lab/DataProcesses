# FilterFS Block

Applies a configurable stateful low-pass, high-pass, band-pass, band-stop, or notch filter to Fast Stream time-series input.

## Presentation

| Field | Value |
|---|---|
| Title | `FilterFS` |
| Subtitle | `Fast Stream Filter` |
| Icon | Reuses `FilterTS/icon.png`. |

## Ports

| ID | Direction | Family | Required | Schema |
|---|---|---|---:|---|
| `input` | Input | Fast Stream | Yes | `FixedRateTimeSeries1D` (FS) (`FastStreamFrame`) |
| `output` | Output | Fast Stream | Yes | `FixedRateTimeSeries1D` (FS) (`FastStreamFrame`) |

## Settings

Block-specific settings use this JSON shape:

```json
{
  "filterType": "lowPass",
  "cutoffFrequencyHertz": 5.0,
  "lowerCutoffFrequencyHertz": 1.0,
  "upperCutoffFrequencyHertz": 10.0,
  "notchBandwidthHertz": 2.0,
  "order": 2
}
```

| Field | Type | Default | Notes |
|---|---|---|---|
| `filterType` | string | `lowPass` | `lowPass`, `highPass`, `bandPass`, `bandStop`, or `notch`. |
| `cutoffFrequencyHertz` | number | `5.0` | LPF/HPF cutoff or Notch center frequency. Must be positive and below Nyquist at runtime. |
| `lowerCutoffFrequencyHertz` | number | `1.0` | Lower cutoff for band-pass and band-stop modes. |
| `upperCutoffFrequencyHertz` | number | `10.0` | Upper cutoff for band-pass and band-stop modes; must be greater than the lower cutoff and below Nyquist at runtime. |
| `notchBandwidthHertz` | number | `2.0` | Notch bandwidth. The center frequency must exceed half the bandwidth; the resulting upper cutoff must be below Nyquist at runtime. |
| `order` | integer | `2` | Cascade count for the one-pole sections. Accepted range is 2 through 10. |

The Inspector shows only the frequency fields relevant to the selected mode. Notch is implemented as a band-stop filter whose bounds are the center frequency plus/minus half the bandwidth.

## Fast Stream processing

Each channel is filtered independently. Frames without a positive `SampleRateHertz` are rejected (ADR 0005). Filter state is retained between packets and reset when the node stops. The output preserves the input frame's timing metadata, exact integer sample rate, channel names, and sequence number.