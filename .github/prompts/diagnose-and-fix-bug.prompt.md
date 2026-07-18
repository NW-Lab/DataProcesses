---
mode: agent
description: Reproduce, diagnose, fix, and validate a DataProcesses bug without concealing the root cause.
---

Diagnose and fix the reported DataProcesses defect. First read `.github/copilot-instructions.md`, `docs/development-workflow.md`, the relevant specification and accepted ADRs, the affected source and tests, and one comparable working implementation. Load `dataprocesses-desktop-development` for application, Avalonia, startup, or desktop composition work. Also load `dataprocesses-node-development` for Block, port, packet, processing, or plugin-contract work.

Before editing, write a concise diagnosis plan containing the reported symptom, smallest reproduction, expected behavior, likely subsystem, evidence to collect, and the acceptance test. If the application must be started in Visual Studio, use `DataProcesses.Desktop` as the startup project; never try to launch a class library or test project with F5.

Classify the failure before changing code: build, test, startup, binding, layout, threading, data-contract, execution, or functional behavior. Capture the actual exception, logs, inputs, and configuration. Do not disable warnings, analyzers, tests, bindings, exceptions, or error handling to make the symptom disappear. Preserve project boundaries and avoid unrelated refactoring.

Implement the smallest coherent root-cause fix. Add or update a deterministic regression test using synthetic data where practical. For a user-visible change, include precise manual verification steps and verify keyboard access, focus, accessible naming, non-color cues, and light/dark themes as relevant. Update documentation, localization resources, or ADRs if behavior, public compatibility, or architecture changes.

Run the complete Release build and test suite. Report the root cause, changed files, automated validation, manual verification, compatibility impact, and remaining limitations. Do not claim the issue is fixed when the build or tests fail.
