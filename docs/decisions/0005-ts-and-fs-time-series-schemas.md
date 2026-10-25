# ADR 0005: Separate TS and FS time-series schemas

**Status:** Accepted  
**Date:** 2026-10-25

## Context

The first time-series Blocks targeted Arduino-style input: CSV rows such as `millis,data` over serial or BLE, timed by source timestamps or a nominal `SamplePeriodNanoseconds`. ADR 0004 added `SampleRateHertz` to support exact fixed rates such as 44.1 kHz audio, and FS Blocks (TestSignalFS, MicInputFS, AudioOutFS, FilterFS, FftFS, StreamChartFS, StreamOutputFS, BleInputFS) were added on top of it.

Both families still declared `PortDataSchema.TimeSeries1D`, so graph validation could not tell them apart. A TS source could be connected to an FS consumer that assumes an exact sample rate, and the abbreviations were overloaded: `FS` meant both "all Fast Stream ports" in the palette and "fixed-rate Block" in Block names, while TS Blocks were inconsistently suffixed `TS` or `St`.

## Decision

Define two distinct time-series schemas within the Fast Stream family. Both use `FastStreamFrame`.

| Abbreviation | Schema | Meaning |
|---|---|---|
| TS (Time Series) | `TimeSeries1D` (value 1, unchanged) | Timed by source timestamps or a nominal sample period. `SampleRateHertz` is optional. |
| FS (Fast Speed Time Series) | `FixedRateTimeSeries1D` (value 7, new) | Fixed integer sample rate. Every frame must set `SampleRateHertz`. |

TS and FS ports are strictly incompatible in both directions. Crossing between them requires an explicit conversion Block such as a resampler. Generic processors (FastFourierTransform, LowPassFilter, MovingAverage, CsvInput, CsvOutput) remain TS.

The Flow Editor uses `TS` and `FS` as port badges and palette filters for these two schemas. The palette no longer offers a filter for the entire Fast Stream family; the blue circular `S` port remains the family cue.

Built-in TS Blocks use the `TS` suffix in display names, directories, and C# type names (for example, `FilterTS`, `SerialInputTS`). Their type IDs, port IDs, settings keys, and JSON Message topics are unchanged.

## Compatibility

`FixedRateTimeSeries1D` is appended to the public enum. Existing values keep their numbers, so Add-ins that only use the existing schemas continue to compile and behave the same. Add-ins that produce fixed-rate streams should declare `FixedRateTimeSeries1D` and always set `SampleRateHertz`.

Saved Flows remain loadable because type IDs are unchanged. Connections saved before this ADR record FS source ports as `TimeSeries1D`. The validator accepts that legacy value when the current source port is FS and the current ports are otherwise compatible, and the Flow Editor upgrades the recorded schema when the Flow is loaded. A saved TS → FS connection, such as TestSignal (TS) → StreamOutputFS, is now reported as an incompatible connection and must be reconnected to an FS source.

C# code that referenced renamed TS Block types (for example, `FilterStBlock`) must use the new names (`FilterTSBlock`).

## Consequences

Graph validation now guarantees that FS consumers receive frames from FS producers. Built-in FS consumers also reject, with `ArgumentException`, any frame whose `SampleRateHertz` is missing or not positive; they do not derive a rate from `SamplePeriodNanoseconds`. A TS → FS resampling Block is needed to use Arduino-style input with FS processing.
