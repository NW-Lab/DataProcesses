# Moving Average Block

Smooths each Fast Stream channel using a moving window specified by sample count or elapsed time.

## Ports

| ID | Direction | Family | Required | Schema |
|---|---|---|---:|---|
| `input` | Input | Fast Stream | Yes | `TimeSeries1D` channel-major numeric samples. |
| `output` | Output | Fast Stream | Yes | `TimeSeries1D` with original timing, channel names, and sequence number. |

## Settings

```json
{
  "windowMode": "samples",
  "windowSize": 10,
  "windowDurationMilliseconds": 100.0
}
```

| Field | Type | Default | Notes |
|---|---|---|---|
| `windowMode` | string | `samples` | `samples` averages the latest `windowSize` samples; `duration` averages samples in the latest elapsed-time window. |
| `windowSize` | integer | `10` | Positive number of samples when `windowMode` is `samples`. |
| `windowDurationMilliseconds` | number | `100.0` | Positive elapsed-time window when `windowMode` is `duration`. |

## Fast Stream processing

The Block preserves frame metadata and maintains independent state for every channel across frame boundaries. A time window includes samples whose timestamp is within the configured duration of the current sample. Time mode requires a positive `SamplePeriodNanoseconds`. State resets when execution stops, the channel count changes, or timestamps move backwards.