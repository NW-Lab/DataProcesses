# MicInputFS Block

Captures a block of microphone samples from the Windows default recording device and emits sample-rate-aware Fast Stream frames.

## Presentation

| Field | Value |
|---|---|
| Title | `MicInputFS` |
| Subtitle | `Microphone Fast Stream` |
| Icon | Reuses the TestSignalFS source icon. |

## Ports

| ID | Direction | Family | Required | Schema |
|---|---|---|---:|---|
| `stream` | Output | Fast Stream | Yes | `FixedRateTimeSeries1D` (FS) (`FastStreamFrame`) |

## Settings

| Field | Type | Default | Notes |
|---|---|---|---|
| `sampleRateHertz` | integer | `44100` | Shared-mode WASAPI capture is requested at this rate; valid range is 8000 to 192000 Hz. |
| `channelMode` | string | `Stereo` | `Stereo` emits left/right channels when available; `Left` or `Right` emits one selected channel. A mono endpoint remains one `mono` channel. |

The Block captures 4096 sample frames per Flow run iteration from the Windows default recording device. It uses WASAPI shared-mode format conversion to the selected rate. The sample values are normalized float-range values stored as `double`.

## Fast Stream output / processing

Each output frame carries the capture timestamp, the exact requested integer `SampleRateHertz`, and `left`/`right`, selected-channel, or `mono` channel names. When a capture endpoint does not provide stereo channels, selecting Left or Right uses the available mono channel.