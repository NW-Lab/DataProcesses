# HartRateImage Block

Predicts heart rate from sequential image frames using a simplified remote photoplethysmography pipeline inspired by `NW-Lab/face_blood`.

## Presentation

| Field | Value |
|---|---|
| Title | `HartRateImage` |
| Subtitle | `rPPG heart rate` |
| Icon | `icon.png`, 64 x 64 PNG source rendered at 32 x 32 in the Node Library and 28 x 28 on the canvas. |

## Dashboard

HartRateImage is not shown on the dashboard by default. Connect `heart-rate` to StreamChartSt or another compatible Fast Stream output Block to inspect the BPM estimate.

## Ports

| ID | Direction | Family | Required | Schema |
|---|---|---|---:|---|
| `image-in` | Input | Fast Stream | Yes | RGB, RGBA, or Gray image frames. The central ROI is used for color averaging. |
| `heart-rate` | Output | Fast Stream | Yes | One-sample time-series frame named `heart-rate-bpm`. |

## Settings

Block-specific settings use this JSON shape:

```json
{
  "regionScale": 0.55,
  "minimumSampleCount": 64,
  "windowSeconds": 12.0,
  "minimumHeartRateBpm": 42.0,
  "maximumHeartRateBpm": 210.0,
  "defaultFrameRateHertz": 30.0
}
```

| Field | Type | Default | Notes |
|---|---|---|---|
| `regionScale` | number | `0.55` | Fraction of image width and height used as the centered ROI. |
| `minimumSampleCount` | integer | `64` | Minimum image-frame history before BPM is emitted. |
| `windowSeconds` | number | `12.0` | Maximum retained RGB history for spectral analysis. |
| `minimumHeartRateBpm` | number | `42.0` | Lower frequency-band edge. |
| `maximumHeartRateBpm` | number | `210.0` | Upper frequency-band edge. |
| `defaultFrameRateHertz` | number | `30.0` | Timestamp fallback when incoming image frames do not include `Timestamp`. |

## Fast Stream output / processing

Each incoming image produces one output `FastStreamFrame`. Values are `NaN` until enough image samples exist. After that, the Block estimates BPM from the dominant frequency in the configured heart-rate band.

Processing follows the same practical shape as the referenced Face Blood rPPG pipeline: central ROI RGB averaging, POS projection (`X = G - B`, `Y = G + B - 2R`), mean removal, Hann windowing, and frequency-domain peak search. This is a lightweight analysis Block for experimentation and is not intended for medical use.