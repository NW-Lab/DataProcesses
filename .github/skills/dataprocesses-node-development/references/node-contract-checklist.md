# Node contract checklist

Use this reference while designing or reviewing a DataProcesses node. Record decisions in code, tests, or user documentation rather than leaving them only in a pull-request conversation.

## Identity and presentation

| Item | Required decision |
|---|---|
| Node type ID | Stable, namespaced identifier used for persistence and discovery |
| Version | Contract or implementation version and compatibility behavior |
| Display name | Localization resource key, not a hard-coded user-facing string |
| Category | Palette grouping and search terms |
| Description | One responsibility expressed in user language |

## Ports

For every port, define a stable ID, direction, cardinality, data family, detailed schema, units, optionality, and behavior when disconnected. Confirm that graph validation can reject an invalid connection before execution.

A Fast Stream schema should state whether the frame is time series, spectrum, or another numeric representation. Define channel names and units, numeric type, sample rate or frequency bins, timing origin, discontinuity semantics, and buffer ownership.

A JSON Message schema should state the envelope version, message type, required and optional payload fields, maximum expected size, correlation or timestamp fields when needed, and unknown-field behavior.

## Settings and lifecycle

Define defaults, valid ranges, cross-field validation, whether a setting may change while running, reset behavior, cancellation behavior, disposal, thread-safety expectations, and state persistence. Avoid locale-dependent numeric or date formats in persisted settings.

## Fast Stream timing

For regular samples, prefer one frame origin plus a sample period rather than one timestamp object per sample. Preserve sufficient precision internally. For explicit CSV interchange, document whether `millis` is relative to recording start or an absolute Unix time and use an additional format such as `time_ns` when millisecond precision is insufficient.

For irregular samples, define an explicit offset or timestamp vector. Define behavior for non-monotonic time, gaps, overlaps, rate changes, and empty frames.

## Buffer ownership

Identify who allocates, who may mutate, how long the buffer remains valid, and who releases or returns pooled memory. Do not retain borrowed buffers after processing. Copy only when ownership or asynchronous lifetime requires it, and make the cost visible.

## Tests

| Area | Minimum coverage |
|---|---|
| Metadata | Stable node ID, port IDs, directions, families, and schemas |
| Configuration | Defaults, boundaries, invalid combinations, serialization if applicable |
| Processing | Known synthetic input and expected output with stated tolerance |
| Timing | Origin, period, gaps, empty frame, and sample-rate changes as applicable |
| Lifecycle | Cancellation, reset, disposal, and repeated execution |
| Errors | Actionable failure without swallowed exceptions or partial corrupt output |
| Compatibility | Rejection of incompatible ports and preservation of supported saved forms |
| Performance | Allocation and throughput evidence for material hot-path changes |

Use sine waves, impulses, steps, white-noise sequences with fixed seeds, and short hand-computable vectors. Never include real personal, health, biometric, or production acquisition data.
