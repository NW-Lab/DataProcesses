# FilterSt Block

Applies a configurable cascaded one-pole filter to Fast Stream time-series input.

## Presentation

| Field | Value |
|---|---|
| Title | `FilterSt` |
| Subtitle | `Stream filter` |
| Icon | `icon.png`, 64 x 64 PNG source rendered at 32 x 32 in the Node Library and 28 x 28 on the canvas. |

## Ports

| ID | Direction | Family | Required | Schema |
|---|---|---|---:|---|
| `input` | Input | Fast Stream | Yes | `TimeSeries1D` (`FastStreamFrame`) |
| `output` | Output | Fast Stream | Yes | `TimeSeries1D` (`FastStreamFrame`) |

## Settings

Block-specific settings use this JSON shape:

```json
{
  "filterType": "lowPass",
  "cutoffFrequencyHertz": 5.0,
  "lowerCutoffFrequencyHertz": 1.0,
  "upperCutoffFrequencyHertz": 10.0,
  "order": 2
}
```

| Field | Type | Default | Notes |
|---|---|---|---|
| `filterType` | string | `lowPass` | `lowPass`, `highPass`, `bandPass`, or `bandStop`. |
| `cutoffFrequencyHertz` | number | `5.0` | Cutoff frequency for low-pass and high-pass modes. Must be positive and below Nyquist at runtime. |
| `lowerCutoffFrequencyHertz` | number | `1.0` | Lower cutoff for band-pass and band-stop modes. Must be positive. |
| `upperCutoffFrequencyHertz` | number | `10.0` | Upper cutoff for band-pass and band-stop modes. Must be greater than the lower cutoff and below Nyquist at runtime. |
| `order` | integer | `2` | Cascade count for the one-pole sections. Accepted range is 2 through 10. |

## Fast Stream processing

Each channel is filtered independently and emitted as a `FastStreamFrame` with the original timing metadata, channel names, and sequence number. Filter state is retained between packets and reset when the node stops.