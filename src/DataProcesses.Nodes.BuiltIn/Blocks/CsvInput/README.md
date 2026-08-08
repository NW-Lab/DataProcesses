# CSV Input Block

Reads CSV data from a file or COM source and emits Fast Stream time-series outputs.

## Presentation

| Field | Value |
|---|---|
| Title | `CsvInput` |
| Subtitle | `File/COM CSV` |
| Icon | `icon.png`, 64 x 64 PNG source rendered at 32 x 32 in the Node Library and 28 x 28 on the canvas. |

## Dashboard

CSV Input is shown on the dashboard by default.

| Setting | Default |
|---|---:|
| Show on Dashboard | `true` |
| Dashboard width | `2` grid cells |
| Dashboard height | `1` grid cell |

## Ports

CSV Input is a source Block. It has no input ports.

| ID | Direction | Family | Required | Schema |
|---|---|---|---:|---|
| `stream-1` .. `stream-16` | Output | Fast Stream | No | `TimeSeries1D` (`FastStreamFrame`) |

The runtime uses `outputCount` from settings to choose how many output ports are active.

## Settings

Block-specific settings use this JSON shape:

```json
{
  "outputCount": 2,
  "sourceType": "File",
  "filePath": "",
  "filePlaybackMode": "Immediate",
  "comPortName": "COM3",
  "baudRate": 115200,
  "hasHeaderRow": true
}
```

See `CsvInputSettings` for full validation and enum values.

## Data examples

Typical CSV input shape:

```csv
millis,CH1,CH2
0,0.0,1.0
1,0.1,0.9
```

Typical output is one `FastStreamFrame` per active stream output, using `TimeSeries1D` schema.
