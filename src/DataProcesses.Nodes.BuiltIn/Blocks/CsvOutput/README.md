# CSV Output Block

Writes Fast Stream time-series input into CSV files.

## Presentation

| Field | Value |
|---|---|
| Title | `CsvOutput` |
| Subtitle | `File sink` |
| Icon | `icon.png`, 64 x 64 PNG source rendered at 32 x 32 in the Node Library and 28 x 28 on the canvas. |

## Dashboard

CSV Output is shown on the dashboard by default.

| Setting | Default |
|---|---:|
| Show on Dashboard | `true` |
| Dashboard width | `2` grid cells |
| Dashboard height | `1` grid cell |

## Ports

| ID | Direction | Family | Required | Schema |
|---|---|---|---:|---|
| `input` | Input | Fast Stream | Yes | `TimeSeries1D` (`FastStreamFrame`) |

CSV Output is a sink Block and has no output ports.

## Settings

Block-specific settings use this JSON shape:

```json
{
  "filePath": "output.csv",
  "writeMode": "Append",
  "spanMilliseconds": 100,
  "executionSessionId": 0,
  "inputBindings": []
}
```

See `CsvOutputSettings` for full validation and binding details.

## Data examples

Typical input is a `FastStreamFrame` with one or more channels. The file format uses CSV rows such as:

```csv
millis,CH1,CH2
0,0.0,1.0
1,0.1,0.9
```
