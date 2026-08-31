# BreathImage Block

BreathImage predicts respiratory rate from sequential image frames using a lightweight image-based respiration pipeline inspired by `NW-Lab/face_breath`.

## Presentation

| Field | Value |
|---|---|
| Title | `BreathImage` |
| Subtitle | `Image respiration` |
| Icon | `icon.png`, 64 x 64 PNG source rendered at 32 x 32 in the Node Library and 28 x 28 on the canvas. |

## Dashboard

BreathImage is not shown on the dashboard by default. Connect `breath-rate` to StreamChartSt or another compatible Fast Stream output Block to inspect the breaths-per-minute estimate.

## Ports

| ID | Direction | Family | Required | Schema |
|---|---|---|---:|---|
| `image-in` | Input | Fast Stream | Yes | RGB, RGBA, or Gray image frames. RGB/RGBA frames use the centered ROI for Cg extraction. |
| `breath-rate` | Output | Fast Stream | Yes | One-sample time-series frame named `breath-rate-brpm`. |

## Settings

Block-specific settings use this JSON shape:

```json
{
  "regionScale": 0.55,
  "minimumSampleCount": 90,
  "windowSeconds": 20.0,
  "minimumBreathRateBpm": 6.0,
  "maximumBreathRateBpm": 30.0,
  "defaultFrameRateHertz": 30.0
}
```

| Field | Type | Default | Notes |
|---|---|---|---|
| `regionScale` | number | `0.55` | Fraction of image width and height used as the centered ROI. |
| `minimumSampleCount` | integer | `90` | Minimum image-frame history before breaths per minute is emitted. |
| `windowSeconds` | number | `20.0` | Maximum retained Cg-channel history for spectral analysis. |
| `minimumBreathRateBpm` | number | `6.0` | Lower respiratory frequency-band edge. |
| `maximumBreathRateBpm` | number | `30.0` | Upper respiratory frequency-band edge. |
| `defaultFrameRateHertz` | number | `30.0` | Timestamp fallback when incoming image frames do not include `Timestamp`. |

## Fast Stream output / processing

Each incoming image produces one output `FastStreamFrame`. Values are `NaN` until enough image history exists, then contain the dominant respiratory frequency in breaths per minute.

The current implementation follows the repository-friendly portion of `face_breath`: central ROI RGB averaging, YCgCo Cg extraction (`Cg = G - (R + B) / 2`), linear detrending, Hann-windowed DFT, and peak search in the 0.1-0.5 Hz respiratory band. It does not use MediaPipe FaceMesh or emit nose/mouth breathing classification yet. This Block is for experimentation and is not intended for medical diagnosis.