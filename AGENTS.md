# DataProcesses agent guide

Work from the repository specification and accepted ADRs. Read `.github/copilot-instructions.md` and any matching file in `.github/instructions/` before editing. Use the `dataprocesses-node-development` skill for node, port, processing, and plugin-contract work.

Preserve project boundaries: plugin abstractions must not depend on Avalonia; core behavior must not depend on desktop UI; built-in nodes must use the public contracts. Keep Fast Stream as typed numeric frames and JSON Message as versioned event or control payloads. CSV belongs at explicit boundaries, not between processing nodes.

Make the smallest coherent change, add deterministic synthetic-data tests, update documentation when behavior or contracts change, and run the full Release build and tests before completion. Never commit secrets or real personal, biometric, health, or production acquisition data.
