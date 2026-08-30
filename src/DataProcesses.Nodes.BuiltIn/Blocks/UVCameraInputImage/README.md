# UVCameraInputImage Block

Captures sequential RGB24 image frames from a UV camera while playback is enabled.

## Presentation

| Field | Value |
|---|---|
| Title | `UVCameraInputImage` |
| Subtitle | `UV camera stream` |
| Icon | `icon.png`, 64 x 64 PNG source rendered in the Node Library and canvas. |

## Ports

| ID | Direction | Family | Required | Schema |
|---|---|---|---:|---|
| `control` | Input | JSON Message | No | Payload object with boolean `isPlay`. |
| `image` | Output | Fast Stream | Yes | Sequential RGB24 `ImageFrame` values. |

## Settings

```json
{
  "deviceIndex": 0,
  "width": 1920,
  "height": 1080,
  "fps": 10.0,
  "isPlay": true,
  "isWhiteBalanceAuto": true,
  "whiteBalanceTemperature": 4500
}
```

`width` and `height` can request up to 3840 x 2160. The output uses the actual resolution returned by the camera. Some UV cameras do not expose a UVC interface and cannot be opened by camera index.

## Payload input / output

Send a JSON Message with `{ "isPlay": true }` to start and `{ "isPlay": false }` to pause capture. An `isPlay` value must be a JSON boolean.