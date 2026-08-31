# HumansImage Block

Counts face-like regions in an `ImageFrame` and emits the number of detected people as a one-sample Fast Stream frame.

## Presentation

| Field | Value |
|---|---|
| Title | `HumansImage` |
| Subtitle | `Face count` |
| Icon | `icon.png`, 64 x 64 PNG source rendered at 32 x 32 in the Node Library and 28 x 28 on the canvas. |

## Dashboard

This Block does not create a dashboard widget by default. Connect its Fast Stream output to a stream or chart output Block to inspect counts.

## Ports

| ID | Direction | Family | Required | Schema |
|---|---|---|---:|---|
| `image-in` | Input | Fast Stream | Yes | `ImageFrame` with `Image2D` schema. RGB24 and RGBA32 images are analyzed; Gray8 produces a count of `0`. |
| `humans-count` | Output | Fast Stream | Yes | One-channel `FastStreamFrame` with channel `humans-count`; the single sample is the detected person count. |

## Settings

Block-specific settings use this JSON shape:

```json
{
  "minimumFacePixelCount": 16,
  "minimumFaceWidthPixels": 3,
  "minimumFaceHeightPixels": 3,
  "minimumSkinRatio": 0.6
}
```

| Field | Type | Default | Notes |
|---|---|---|---|
| `minimumFacePixelCount` | integer | `16` | Smallest connected skin-color region that can be counted as a face candidate. |
| `minimumFaceWidthPixels` | integer | `3` | Minimum bounding-box width for a face candidate. |
| `minimumFaceHeightPixels` | integer | `3` | Minimum bounding-box height for a face candidate. |
| `minimumSkinRatio` | number | `0.6` | Minimum filled ratio of skin-color pixels inside the candidate bounding box. |

## Fast Stream processing

Each input image is processed independently. The Block classifies RGB/RGBA pixels with conservative skin-color thresholds, groups adjacent matching pixels into connected components, filters by size, aspect ratio, and fill ratio, then emits the number of remaining face candidates.

This implementation is deterministic and does not load an external face-detection model. It is intended as an initial built-in Block contract and synthetic-testable baseline; future model-based recognition should keep the same ports unless a compatibility record supersedes this contract.