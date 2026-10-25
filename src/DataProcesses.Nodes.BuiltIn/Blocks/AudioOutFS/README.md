# AudioOutFS Block

Plays mono or stereo Fast Stream data through the Windows default audio output and can record the same stream to WAV.

## Presentation

| Field | Value |
|---|---|
| Title | `AudioOutFS` |
| Subtitle | `Fast Stream Audio Output` |
| Icon | Reuses the StreamOutputFS icon. |

## Dashboard

The Dashboard widget has two controls: Mute/Unmute and Start Recording/Stop Recording. Muting silences playback only; WAV recording continues. Recording starts and stops only from the Dashboard.

## Ports

| ID | Direction | Family | Required | Schema |
|---|---|---|---:|---|
| `input` | Input | Fast Stream | Yes | `FixedRateTimeSeries1D` (FS) (`FastStreamFrame`), mono or stereo. |

## Inspector settings

| Field | Type | Default | Notes |
|---|---|---|---|
| `recordingFolder` | string | User Music/DataProcesses | Folder for recorded WAV files. |
| `fileNamePrefix` | string | `audio` | Prefix followed by UTC time and a unique suffix. |

## Fast Stream input and audio output

Each frame must contain one or two equal-length channels and a sample rate between 8 and 192 kHz. The integer `SampleRateHertz` is required; frames without it are rejected (ADR 0005). Samples are clamped to [-1, 1] and rendered as IEEE float audio through shared-mode WASAPI. The output device follows the Windows default playback endpoint.

When recording is active, the same interleaved float samples are written to a 32-bit float WAV file. A format or destination change starts a new file. Playback and the writer are released after the stream has been idle for two seconds.