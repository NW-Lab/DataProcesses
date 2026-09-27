# TestSignalFS Block

Generates a fixed-size Fast Stream signal with an exact integer sample rate.

## Presentation

| Field | Value |
|---|---|
| Title | `TestSignal(FS)` |
| Title | `TestSignal(FS)ブロック` |
| Subtitle | `FS` |
| Icon | Reuses the TestSignalTS signal icon. |

## Dashboard

No dashboard widget is created by default. Connect `StreamOutputFS` to inspect the generated frequency and downsampled values.

## Ports

| ID | Direction | Family | Required | Schema |
|---|---|---|---:|---|
| `stream` | Output | Fast Stream | Yes | `TimeSeries1D` (`FastStreamFrame`) |

## Settings

```json
{
  "waveType": "sine",
  "frequency": 440.0,
  "peakIntensity": 1.0,
  "sampleRateHertz": 44100
}
```

| Field | Type | Default | Notes |
|---|---|---:|---|
| `waveType` | string | `sine` | `sine` or `square`. |
| `frequency` | number | `440.0` | Positive signal frequency; must not exceed half the sample rate. |
| `peakIntensity` | number | `1.0` | Non-negative peak value; square waves emit positive and negative peaks. |
| `sampleRateHertz` | integer | `44100` | Sampling frequency from 1 Hz through 1 MHz. |

## Fast Stream output / processing

The Block emits one `FastStreamFrame` containing 256 samples on channel `signal`. `SampleRateHertz` is set exactly; `SamplePeriodNanoseconds` carries its rounded nominal period for compatibility. The frame start timestamp uses millisecond resolution from the system clock. No CSV `millis` channel or JSON control port is emitted.