---
mode: agent
description: Design and implement a DataProcesses processing node with contracts, tests, and documentation.
---

Create or refine the requested DataProcesses Block. First load the `dataprocesses-node-development` skill and read the repository specification, accepted ADRs, public abstractions, `src/DataProcesses.Nodes.BuiltIn/Blocks/README.md`, one similar built-in Block, and matching tests.

Before editing, present a concise contract table covering the node type ID, responsibility, input and output port IDs, directions, Fast Stream or JSON Message family, detailed schemas, units, settings, state, cancellation, errors, and compatibility impact. Resolve ambiguity with the smallest safe assumption and call it out.

For a built-in Block, create `src/DataProcesses.Nodes.BuiltIn/Blocks/<BlockName>/` and keep the stable definition, runtime implementation, and factory in separate `<BlockName>Block.cs`, `<BlockName>Node.cs`, and `<BlockName>NodeFactory.cs` files. Register the factory explicitly in `BuiltInNodePlugin` and mirror tests in `tests/DataProcesses.Nodes.BuiltIn.Tests/Blocks/<BlockName>/`.

Implement the node through `DataProcesses.Plugin.Abstractions`, keep processing independent of Avalonia, and preserve timing and buffer ownership. Do not convert Fast Stream frames to CSV or JSON internally. Add deterministic synthetic-data tests for metadata, validation, valid output, edge cases, and cancellation. Add a benchmark or measurable allocation check if the change materially affects a hot path.

Update relevant documentation and localization resources. Run the Release build and test suite, then report changed files, contract decisions, validation results, and remaining risks. Do not claim success if the build or tests fail.
