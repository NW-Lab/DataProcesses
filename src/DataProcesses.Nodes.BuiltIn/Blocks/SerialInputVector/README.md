# SerialInputVector Block

Reads one IMU X/Y/Z vector per Arduino USB serial row.

## Presentation

| Field | Value |
|---|---|
| Title | `SerialInputVector` |
| Subtitle | `IMU XYZ CSV` |
| Icon | Shared `SerialInputSt` serial-input icon. |

## Ports

| ID | Direction | Family | Required | Schema |
|---|---|---|---:|---|
| `vector` | Output | Fast Stream | No | `NumericVector1D` (`NumericVectorFrame`) containing X, Y, and Z in that order. |

## Settings

```json
{
  "comPortName": "COM3",
  "baudRate": 115200
}
```

## Vector output

Arduino must send newline-delimited, invariant-culture CSV rows:

```csv
millis,x,y,z
0,0.01,-0.02,9.81
10,0.02,-0.01,9.80
```

`millis` is relative to the device recording start and becomes the `NumericVectorFrame.Timestamp`. Each row produces one `imu` vector in `[x, y, z]` order, retaining all three values as a single contemporaneous measurement. Rows with invalid values, an unexpected column count, or decreasing `millis` values are rejected.