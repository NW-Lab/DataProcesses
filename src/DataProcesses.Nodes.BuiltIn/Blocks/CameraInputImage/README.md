# CameraInputImage Block

Captures one image from a local camera when a JSON Message trigger or Dashboard Capture action is received.

## Presentation

| Field | Value |
|---|---|
| Title | `CameraInputImage` |
| Subtitle | `Camera capture` |
| Icon | `icon.png`, 64 x 64 PNG source rendered in the Node Library and canvas. |

## Dashboard

The Block is shown by default. Its `Capture` button requests one camera frame during the next flow execution.

## Ports

| ID | Direction | Family | Required | Schema |
|---|---|---|---:|---|
| `trigger` | Input | JSON Message | No | Payload object with boolean `Trigger`. Only `true` captures an image. |
| `image` | Output | Fast Stream | Yes | `ImageFrame` with an interleaved RGB24 image. |

## Settings

```json
{
  "deviceIndex": 0,
  "width": 1920,
  "height": 1080,
  "continuousCapture": false,
  "fps": 10.0,
  "isWhiteBalanceAuto": true,
  "whiteBalanceTemperature": 4500
}
```

| Field | Type | Default | Notes |
|---|---|---:|---|
| `deviceIndex` | integer | 0 | Zero-based local camera device index. |
| `width` | integer | 1920 | Requested capture width from 1 through 3840 pixels. |
| `height` | integer | 1080 | Requested capture height from 1 through 2160 pixels. |
| `continuousCapture` | boolean | false | Emits repeatedly while the Flow is running. |
| `fps` | number | 10 | Continuous capture rate from 0.1 through 60 FPS. |
| `isWhiteBalanceAuto` | boolean | true | Requests automatic camera white balance. |
| `whiteBalanceTemperature` | number | 4500 | Manual white-balance temperature from 2000 through 10000 K. |

## Payload input / output

Send a JSON Message whose `payload` is:

```json
{ "Trigger": true }
```

`Trigger` is case-sensitive and must be the JSON boolean `true`; all other payloads are ignored.

## Fast Stream output / processing

Every capture requests the configured resolution and white-balance mode from the local camera, reads one frame, converts it from BGR to RGB24, and emits an `ImageFrame` named `camera`. The output dimensions are the dimensions actually returned by the camera, which can be lower when the requested mode is unsupported. The copied byte buffer is owned by the frame, so it remains valid after the camera handle is released.