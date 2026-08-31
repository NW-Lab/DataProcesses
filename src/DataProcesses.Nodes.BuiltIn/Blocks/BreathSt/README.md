# BreathSt Block

BreathSt detects respiratory cycles from a one-channel Fast Stream signal and emits breaths per minute plus optional anomaly events.

## Presentation

| Field | Value |
|---|---|
| Title | `BreathSt` |
| Subtitle | `Respiration rate` |
| Icon | `icon.png`, 64 x 64 PNG source rendered at 32 x 32 in the Node Library and 28 x 28 on the canvas. |

## Ports

| ID | Direction | Family | Required | Schema |
|---|---|---|---:|---|
| `stream-in` | Input | Fast Stream | Yes | One-dimensional time-series. The first channel is analyzed. |
| `breath-rate` | Output | Fast Stream | Yes | One channel named `breath-rate-brpm`, preserving input timing and sequence number. |
| `events` | Output | JSON Message | No | Emits signal-derived anomaly events such as cough-like spikes. |

## Settings

Block-specific settings use this JSON shape:

```json
{
  "method": "breathBelt",
  "emitAnomalyEvents": true,
  "peakThresholdFraction": 0.55,
  "coughSpikeThresholdFraction": 0.75,
  "minimumBreathIntervalMilliseconds": 1500,
  "maximumBreathIntervalMilliseconds": 10000,
  "coughRefractoryMilliseconds": 1000
}
```

| Field | Type | Default | Notes |
|---|---|---|---|
| `method` | string | `breathBelt` | Inspector-selectable method. Use `breathBelt` for respiration belts and `ledOxygen` for LED / SpO2-like respiratory modulation. |
| `emitAnomalyEvents` | boolean | `true` | Enables JSON anomaly output. |
| `peakThresholdFraction` | number | `0.55` | Fraction between the finite frame minimum and maximum used for breath peak detection. |
| `coughSpikeThresholdFraction` | number | `0.75` | Multiplier of finite frame range used for cough-like spike detection. |
| `minimumBreathIntervalMilliseconds` | number | `1500` | Rejects faster cycles as noise. |
| `maximumBreathIntervalMilliseconds` | number | `10000` | Rejects slower cycles as stale intervals. |
| `coughRefractoryMilliseconds` | number | `1000` | Minimum interval between emitted cough-like events. |

## Payload output

Anomaly messages use topic `dataprocesses.breath-st.anomaly`. Payload fields are `eventType`, `method`, `channel`, `sequenceNumber`, `sampleIndex`, `timestampUnixNanoseconds`, and `delta`.

The anomaly output is a signal-quality event detector, not a medical diagnosis. `cough-like-spike` means the input signal contained a sudden amplitude step beyond the configured threshold.

## Fast Stream processing

Breath belt mode detects local maxima. LED oxygen mode inverts the first channel before the same local-peak detector so respiratory dips can be counted. Output samples contain `NaN` until two accepted breaths establish an interval, then hold the latest breaths-per-minute estimate.