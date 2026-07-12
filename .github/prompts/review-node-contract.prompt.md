---
mode: agent
description: Review a DataProcesses node or plugin-contract change for correctness, performance, compatibility, and tests.
---

Review the selected change as a DataProcesses maintainer. Load the `dataprocesses-node-development` skill and compare the change with `docs/initial-specification.md`, accepted ADRs, public abstractions, repository instructions, and tests.

Prioritize findings in this order: data corruption or incorrect timing, unsafe buffer ownership, cancellation or resource leaks, incompatible public or persistence contracts, invalid Fast Stream and JSON Message mixing, missing validation, hot-path allocations or serialization, cross-platform UI violations, accessibility or localization regressions, and insufficient tests.

For each finding, state severity, file and symbol, observable impact, evidence, and the smallest corrective action. Do not report stylistic preferences as defects. Explicitly say when no material findings remain, and list residual risks or validation that could not be performed. Run the Release build and tests when the environment permits.
