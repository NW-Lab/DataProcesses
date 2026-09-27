# ADR 0004: Generic Fast Stream sample-rate timing

**Status:** Accepted  
**Date:** 2026-09-27

## Context

`FastStreamFrame` originally represented regular sampling with an integer `SamplePeriodNanoseconds`. This remains suitable for existing ST sources, but many common rates such as 44.1 kHz have a fractional nanosecond period. Repeatedly using a rounded period introduces timing and frequency error. Fast Stream is also a generic numeric transport, not an audio-specific family.

## Decision

Add optional `SampleRateHertz` metadata to `FastStreamFrame` and `SourceSampleRateHertz` to `SpectrumFrame`. These values are positive integer hertz and are authoritative when present. Sample timestamps are computed from the frame origin and absolute sample index, rather than by accumulating a rounded period. Existing `SamplePeriodNanoseconds` remains the fallback for producers that do not declare a fixed rate.

For example, a 44.1 kHz stream sets `SampleRateHertz` to `44100`; the timestamp of sample index `n` is the frame start plus `floor(n * 1,000,000,000 / 44,100)` nanoseconds. A rate change starts a new frame. This contract supports any regular numeric stream using an integer sample rate; audio is one use case, not a separate data family.

The Flow Editor exposes `FS` as a palette filter for all Fast Stream ports. Existing detailed schemas and the ST CSV/serial input shape remain unchanged.

## Compatibility

The new rate fields are optional and appended to the public record constructors with defaults. Existing Add-ins and saved Flow documents remain source- and data-compatible. Producers that omit the new field continue to use `SamplePeriodNanoseconds`. Timing-aware built-in processors preserve and use the rate when provided.

## Consequences

The nominal integer period remains present for compatibility, but consumers must use the frame timestamp/rate helpers for exact timing when `SampleRateHertz` is set. Variable or irregular sampling is not represented by this field; a rate change requires a frame boundary, and irregular timestamp vectors require a future explicit contract.