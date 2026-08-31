# SerialInputSt Block

Reads Arduino CSV rows from a USB serial port and emits them as a multi-channel Fast Stream.

## Presentation

| Field | Value |
|---|---|
| Title | `SerialInputSt` |
| Subtitle | `Arduino CSV` |

## Ports

| ID | Direction | Family | Required | Schema |
|---|---|---|---:|---|
| `stream` | Output | Fast Stream | No | `TimeSeries1D` (`FastStreamFrame`) with one sample per serial row. |

## Settings

```json
{
  "comPortName": "COM3",
  "baudRate": 115200,
  "channelCount": 2
}
```

| Field | Type | Default | Notes |
|---|---|---:|---|
| `comPortName` | string | `COM3` | USB serial port selected in the Inspector. |
| `baudRate` | integer | `115200` | Must be positive. |
| `channelCount` | integer | `2` | Number of `data` columns, from 1 through 16. |

## Fast Stream output

Arduino must send newline-delimited rows in invariant-culture CSV form:

```csv
millis,data1,data2
0,0.0,1.0
10,0.1,0.9
```

`millis` is relative to the device recording start. It is converted to the frame timestamp; it is not emitted as a data channel. The Block names channels `data1` through `dataN`, preserves each row as one sample, and rejects rows with an unexpected column count, invalid numbers, or decreasing `millis` values.