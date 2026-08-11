# StremOutputTS Block

Receives Fast Stream time-series input and stores a bounded latest snapshot for diagnostics.

## Presentation

| Field | Value |
|---|---|
| Title | `StremOutputTS` |
| Subtitle | `Debug stream` |
| Icon | `icon.png`, 64 x 64 PNG source rendered at 32 x 32 in the Node Library and 28 x 28 on the canvas. |

## Ports

| ID | Direction | Family | Required | Schema |
|---|---|---|---:|---|
| `input` | Input | Fast Stream | Yes | `TimeSeries1D` (`FastStreamFrame`) |

StremOutputTS is a sink Block and has no output ports.

## Settings

This Block currently has no block-specific settings.

## Data examples

Typical input is a `FastStreamFrame` with one or more channels. The preview keeps at most 512 samples per channel after down-sampling.

