# FFT_FS Block

Computes the one-sided magnitude spectrum of every channel in a Fast Stream frame.

## Presentation

| Field | Value |
|---|---|
| Title | `FFT_FS` |
| Subtitle | `Fast Stream Spectrum` |
| Icon | Reuses the FFT icon. |

## Ports

| ID | Direction | Family | Required | Schema |
|---|---|---|---:|---|
| `input` | Input | Fast Stream | Yes | `FixedRateTimeSeries1D` (FS) (`FastStreamFrame`) |
| `spectrum` | Output | Fast Stream | Yes | `Spectrum1D` (`SpectrumFrame`) |

## Fast Stream processing

The Block computes a one-sided amplitude spectrum independently for each channel. Frequency resolution is `SampleRateHertz / sampleCount`. Frames without a positive `SampleRateHertz` are rejected (ADR 0005). The output carries the source timestamp, period, sample rate, channel names, and sequence number. FFT sizing/windowing is unchanged from the existing FFT Block.