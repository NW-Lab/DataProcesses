# BleInputSt Block

BleInputSt receives Arduino-style CSV rows from a BLE GATT notify characteristic and emits them as one multi-channel Fast Stream.

## Presentation

| Field | Value |
|---|---|
| Title | `BleInputSt` |
| Subtitle | `BLE GATT` |
| Icon | `icon.png`, 64 x 64 PNG source rendered at 32 x 32 in the Node Library and 28 x 28 on the canvas. |

## Dashboard

BleInputSt is not shown on the dashboard by default. Connect `stream` to StreamChartSt or another compatible Fast Stream output Block to inspect the received values.

## Ports

| ID | Direction | Family | Required | Schema |
|---|---|---|---:|---|
| `stream` | Output | Fast Stream | No | One or more numeric time-series channels parsed from `millis,data1,data2,...` rows. |

## Settings

Block-specific settings use this JSON shape:

```json
{
  "deviceId": "",
  "deviceName": "",
  "autoConnect": true,
  "serviceUuid": "6e400001-b5a3-f393-e0a9-e50e24dcca9e",
  "notifyCharacteristicUuid": "6e400003-b5a3-f393-e0a9-e50e24dcca9e",
  "channelCount": 2,
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
| `channelCount` | integer | `2` | Number of numeric data columns after `millis`; valid range is 1-16. |
| `timeoutMilliseconds` | integer | `5000` | Disconnects when no notification is received within this interval. |

## Fast Stream output

Each complete notification line must be UTF-8 CSV in the form `millis,data1,data2,...`. `millis` is relative time in milliseconds. The first row anchors the stream to the host timestamp at receipt time. Channel names are generated as `data1`, `data2`, and so on.

The built-in settings default to Nordic UART Service UUID `6e400001-b5a3-f393-e0a9-e50e24dcca9e` and TX notify characteristic UUID `6e400003-b5a3-f393-e0a9-e50e24dcca9e` for Arduino-style Nordic UART sketches. The Windows desktop build scans BLE devices from the Inspector, persists the selected device id and name in `settingsJson`, reconnects to that device on subsequent runs, subscribes to notify or indicate, and unsubscribes when execution stops or the notification timeout elapses.