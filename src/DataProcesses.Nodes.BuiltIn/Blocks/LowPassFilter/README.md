# Low-pass Filter Block

Applies first-order smoothing to Fast Stream time-series input.

## Presentation

| Field | Value |
|---|---|
| Title | `Low-pass Filter` |
| Subtitle | `Smooth stream` |
| Icon | `icon.png`, 64 x 64 PNG source rendered at 32 x 32 in the Node Library and 28 x 28 on the canvas. |

## Ports

| ID | Direction | Family | Required | Schema |
|---|---|---|---:|---|
| `input` | Input | Fast Stream | Yes | `TimeSeries1D` (`FastStreamFrame`) |
| `output` | Output | Fast Stream | Yes | `TimeSeries1D` (`FastStreamFrame`) |

## Settings

This Block currently has no block-specific settings.

## Data examples

Typical input: one `FastStreamFrame` with channel samples.

Typical output: one `FastStreamFrame` with smoothed samples per channel.
