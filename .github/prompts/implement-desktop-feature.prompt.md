---
mode: agent
description: Plan, implement, test, and verify a DataProcesses desktop or Avalonia feature.
---

Implement the requested DataProcesses desktop feature. First load the `dataprocesses-desktop-development` skill and read `.github/copilot-instructions.md`, `docs/development-workflow.md`, the relevant section of `docs/initial-specification.md`, accepted ADRs, matching path instructions, the affected views and view models, and their tests. Load `dataprocesses-node-development` as well if the feature changes a Block, port, packet, plugin contract, or graph-processing behavior.

Before editing, provide a concise implementation plan that identifies the user-visible behavior, affected projects, domain/UI boundary, accessibility and localization requirements, expected states, acceptance criteria, and compatibility effect. Use the smallest safe assumption when requirements are incomplete, and state it explicitly.

Keep business rules, graph execution, persistence, and processing independent of Avalonia. Keep views and view models focused on presentation state, bindings, and commands. Use localization resources for user-visible text. Design keyboard access, focus order, accessible names, responsive layouts, light/dark theme behavior, and non-color state cues from the beginning. Do not introduce Windows-only logic into shared UI or domain paths.

Start and debug the application through `DataProcesses.Desktop`, the only executable project. Do not try to run a class library or test project with F5. Add deterministic tests for behavior changes, and include exact manual visual verification steps when an automated UI test is not practical.

Update documentation and localization resources when needed. Run the complete Release build and test suite. Report changed files, validation results, visual verification, compatibility impact, remaining limitations, and follow-up work. Do not claim completion if the build or tests fail.
