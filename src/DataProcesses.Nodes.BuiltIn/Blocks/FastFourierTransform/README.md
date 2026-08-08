# FFT Block

Converts Fast Stream time-series input into one-sided spectrum output.

## Presentation

| Field | Value |
|---|---|
| Title | `FFT` |
| Subtitle | `Spectrum` |
| Icon | `icon.png`, 64 x 64 PNG source rendered at 32 x 32 in the Node Library and 28 x 28 on the canvas. |

## Ports

| ID | Direction | Family | Required | Schema |
|---|---|---|---:|---|
| `input` | Input | Fast Stream | Yes | `TimeSeries1D` (`FastStreamFrame`) |
| `spectrum` | Output | Fast Stream | Yes | `Spectrum1D` (`SpectrumFrame`) |

## Settings

This Block currently has no block-specific settings.

## Data examples

Typical input: one `FastStreamFrame` carrying time-domain samples.

Typical output: one `SpectrumFrame` carrying frequency-domain magnitudes.
