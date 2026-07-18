# DataProcesses agent guide

Work from the repository specification and accepted ADRs. Read `.github/copilot-instructions.md`, `src/DataProcesses.Nodes.BuiltIn/Blocks/README.md` for built-in Block work, and any matching file in `.github/instructions/` before editing. Use the `dataprocesses-node-development` skill for node, port, processing, and plugin-contract work.

Preserve project boundaries: plugin abstractions must not depend on Avalonia; core behavior must not depend on desktop UI; built-in nodes must use the public contracts. Keep Fast Stream as typed numeric frames and JSON Message as versioned event or control payloads. CSV belongs at explicit boundaries, not between processing nodes.

Place every built-in Block in `src/DataProcesses.Nodes.BuiltIn/Blocks/<BlockName>/`, separating stable definition, runtime execution, and factory into `<BlockName>Block.cs`, `<BlockName>Node.cs`, and `<BlockName>NodeFactory.cs`. Mirror tests under `tests/DataProcesses.Nodes.BuiltIn.Tests/Blocks/<BlockName>/` and register the factory explicitly in `BuiltInNodePlugin`.

Make the smallest coherent change, add deterministic synthetic-data tests, update documentation when behavior or contracts change, and run the full Release build and tests before completion. Never commit secrets or real personal, biometric, health, or production acquisition data.
