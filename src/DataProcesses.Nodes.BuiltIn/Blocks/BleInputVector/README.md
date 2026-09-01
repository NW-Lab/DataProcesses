# BleInputVector Block

BleInputVector receives Arduino-style IMU CSV rows from a BLE GATT notify characteristic and emits each timestamped `x,y,z` sample as one numeric vector.

## Presentation

| Field | Value |
|---|---|
| Title | `BleInputVector` |
| Subtitle | `IMU XYZ BLE` |
| Icon | Uses the BLE input icon rendered at 32 x 32 in the Node Library and 28 x 28 on the canvas. |

## Dashboard

BleInputVector is not shown on the dashboard by default. Connect `vector` to StreamOutputVector, StreamChartVector, or another compatible Fast Stream vector Block to inspect the received IMU values.

## Ports

| ID | Direction | Family | Required | Schema |
|---|---|---|---:|---|
| `vector` | Output | Fast Stream | No | Numeric vector `[x, y, z]` whose elements belong to the same IMU sample timestamp. |

## Settings

Block-specific settings use this JSON shape:

```json
{
  "deviceId": "",
  "deviceName": "",
  "autoConnect": true,
  "serviceUuid": "6e400001-b5a3-f393-e0a9-e50e24dcca9e",
  "notifyCharacteristicUuid": "6e400003-b5a3-f393-e0a9-e50e24dcca9e",
  "timeoutMilliseconds": 5000
}
```

| Field | Type | Default | Notes |
|---|---|---|---|
| `deviceId` | string | empty | Persisted BLE device identifier used for reconnecting on the next run. |
| `deviceName` | string | empty | Human-readable device label shown in the Inspector when available. |
| `autoConnect` | boolean | `true` | When enabled, the host may reconnect to the persisted `deviceId` automatically. |
| `serviceUuid` | string UUID | Nordic UART Service UUID | GATT service to subscribe to. |
| `notifyCharacteristicUuid` | string UUID | Nordic UART TX characteristic UUID | Notify characteristic carrying Arduino CSV text. |
| `timeoutMilliseconds` | integer | `5000` | Disconnects when no notification is received within this interval. |

## Fast Stream output

Each complete notification line must be UTF-8 CSV in the form `millis,x,y,z`. `millis` is relative time in milliseconds. The three vector elements are emitted together in one `NumericVectorFrame` named `imu`, preserving that the values share the same timestamp.