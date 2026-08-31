# CdTimeResolvedMethodSt Block

Calculates a central-difference, time-resolved vector from the first channel of a Fast Stream frame.

## Presentation

| Field | Value |
|---|---|
| Title | `CdTimeResolvedMethodSt` |
| Subtitle | `CD time-resolved vector` |
| Icon | `icon.png`, 64 x 64 PNG source rendered at 32 x 32 in the Node Library and 28 x 28 on the canvas. |

## Ports

| ID | Direction | Family | Required | Schema |
|---|---|---|---:|---|
| `stream-in` | Input | Fast Stream | Yes | Regular `TimeSeries1D` input; only the first channel is processed. |
| `cd-time-resolved` | Output | Fast Stream | Yes | `NumericVector1D` of time derivatives in source-sample order. |

## Processing

For adjacent sample period $\Delta t$, the output uses a forward difference at the first element, central differences for interior elements, and a backward difference at the last element. A one-sample input produces `0`; non-finite source pairs produce `NaN`. The output retains the input sequence number and uses the frame start time as its optional timestamp.