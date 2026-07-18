---
applyTo: "**/*.cs"
---

# C# implementation instructions

Use the repository SDK and language version. Preserve nullable analysis, implicit usings, analyzers, and warnings-as-errors. Prefer simple, explicit code over reflection, dynamic dispatch, or hidden global behavior.

Keep public contracts immutable where practical. Validate public inputs and use `ArgumentException.ThrowIfNullOrWhiteSpace`, `ArgumentNullException.ThrowIfNull`, or domain validation as appropriate. Avoid adding `public` members until a caller requires them.

For Fast Stream code, operate on frames, `ReadOnlyMemory<T>`, `Memory<T>`, or spans where ownership permits. Make buffer ownership explicit. Never retain a span beyond its valid scope, never return pooled buffers without a documented owner, and never introduce per-sample CSV/JSON conversion. Avoid LINQ and closures in measured hot paths.

For JSON Message code, use the current `JsonMessage` envelope: `topic`, arbitrary JSON `payload`, `timestamp`, and optional `correlationId`. Validate the expected payload shape at node boundaries. Treat new envelope fields or preservation rules as explicit public-contract, documentation, and compatibility changes; do not assume a schema-version field exists today. Do not use JSON as a service locator or arbitrary object transport.

Pass `CancellationToken` through execution boundaries. Dispose owned resources deterministically. Do not swallow exceptions; convert expected validation failures into clear domain results and preserve unexpected failures with context.

Add XML documentation to public plugin contracts where intent, ownership, units, threading, or compatibility is not obvious. Add deterministic unit tests for changed behavior and use synthetic data only.
