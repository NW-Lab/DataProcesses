# MovieInputImage Block

Reads sequential RGB24 image frames from a movie file while playback is enabled.

## Presentation

| Field | Value |
|---|---|
| Title | `MovieInputImage` |
| Subtitle | `Movie playback` |
| Icon | `icon.png`, 64 x 64 PNG source rendered in the Node Library and canvas. |

## Ports

| ID | Direction | Family | Required | Schema |
|---|---|---|---:|---|
| `control` | Input | JSON Message | No | Payload object with boolean `isPlay`. |
| `image` | Output | Fast Stream | Yes | Sequential RGB24 `ImageFrame` values. |

## Settings

```json
{
  "moviePath": "C:/data/input.mp4",
  "fps": 10.0,
  "width": 640,
  "height": 480,
  "isPlay": true
}
```

| Field | Type | Default | Notes |
|---|---|---:|---|
| `moviePath` | string | empty | Local path of the movie file. |
| `fps` | number | 10 | Target output rate from 0.1 through 60 FPS. |
| `width` | integer | 640 | Output frame width from 1 through 3840 pixels. |
| `height` | integer | 480 | Output frame height from 1 through 3840 pixels. |
| `isPlay` | boolean | true | Initial state for a flow execution. |

## Payload input / output

Use a JSON Message with this `payload` to start or stop playback:

```json
{ "isPlay": false }
```

Unknown fields are ignored. A supplied `isPlay` value must be a JSON boolean.

## Fast Stream output / processing

Each emitted frame is decoded from the current movie position, converted from BGR to RGB24, resized to the configured width and height, and output as an `ImageFrame` named `movie`. At end of file, the next playback interval starts again at the first frame.