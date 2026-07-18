# Development and debugging workflow

This guide is the operational baseline for contributors and GitHub Copilot when implementing or fixing DataProcesses. It complements the product baseline in [initial-specification.md](initial-specification.md), the accepted architecture decisions in [decisions](decisions/), and the repository-wide instructions in [`.github/copilot-instructions.md`](../.github/copilot-instructions.md).

> **Scope.** DataProcesses is pre-alpha. Prefer the smallest change that makes a reproducible behavior correct, tested, and documented over a broad redesign.

## Choose the right work path

| Change or symptom | Primary area | Read before editing | Minimum validation |
|---|---|---|---|
| The desktop application does not start, a view is incorrect, or an Avalonia binding fails | `src/DataProcesses.Desktop` | This guide, Avalonia instructions, the affected view and view model | Build, tests, and a reproducible visual check |
| A graph, execution, validation, or persistence-neutral rule is wrong | `src/DataProcesses.Core` | Specification, relevant ADR, affected Core tests | Focused test plus full test suite |
| A public packet, port, or plugin contract changes | `src/DataProcesses.Plugin.Abstractions` | Specification, ADRs, current contract consumers | Contract tests, compatibility note, documentation update |
| A built-in Block changes or a new Block is added | `src/DataProcesses.Nodes.BuiltIn/Blocks` | `dataprocesses-node-development` skill and Block layout guide | Mirrored Block tests, registration test, full suite |
| A reported defect has an unknown cause | A minimal reproducer first | This guide and the affected source/test path | A regression test or a documented manual verification |

## Project map and architectural direction

The repository pins the .NET SDK in [`global.json`][1]. The executable desktop project is [`DataProcesses.Desktop.csproj`][2], which has `OutputType` set to `WinExe` and composes the Core, public abstractions, and built-in Block projects.[2]

| Project or path | Responsibility | Start directly with F5? |
|---|---|---|
| `src/DataProcesses.Desktop` | Avalonia application composition, views, view models, and desktop startup | **Yes** |
| `src/DataProcesses.Core` | Flow/domain behavior that is independent of desktop presentation | No; class library |
| `src/DataProcesses.Plugin.Abstractions` | Small, stable public contracts for ports, packets, nodes, and plugins | No; class library |
| `src/DataProcesses.Nodes.BuiltIn` | Built-in Block definitions, factories, and runtime behavior | No; class library |
| `tests/DataProcesses.*.Tests` | Automated unit and contract tests | Run with the test runner or `dotnet test`; do not start with F5 |

Maintain these boundaries: neither `DataProcesses.Plugin.Abstractions` nor `DataProcesses.Core` may depend on the Desktop project or Avalonia. Built-in Blocks use the same public abstractions intended for future Add-ins. Keep Fast Stream as typed numeric data in memory, and use JSON Message for events, control, and variable payloads; use CSV only at explicit import, export, or interoperability boundaries.[3]

## First checkout and command-line validation

Run all commands from the repository root. Use the SDK selected by `global.json`; do not substitute a different target framework merely to make the local environment build.[1]

```bash
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
```

Use a focused test during iteration when it makes the failure faster to diagnose, then run the complete Release build and test suite before claiming completion. Treat a warning, analyzer failure, skipped expected regression test, or failed build as an unresolved result rather than suppressing it.

## Visual Studio and Avalonia startup

Open `DataProcesses.slnx`. In **Solution Explorer**, right-click **`DataProcesses.Desktop`** and select **Set as Startup Project**. Alternatively, open the solution properties, choose **Common Properties → Startup Project**, and select the single startup project `DataProcesses.Desktop`.

> If Visual Studio says that a project with a class-library output type cannot be started directly, the selected startup project is a library or test project. Select `DataProcesses.Desktop` and run again. The desktop entry point calls Avalonia's classic desktop lifetime from [`Program.cs`][4].

Use F5 to debug or Ctrl+F5 to run without attaching the debugger. Reproduce the issue in the smallest stable configuration before editing. In a Debug run, Avalonia developer tools are configured by the application bootstrap; do not remove the debug-only setup merely to hide a visual problem.[4]

## Bug-fix workflow

1. **Capture the evidence.** Record the commit, operating system, exact error or stack trace, inputs, configuration, and the expected versus observed behavior. Remove secrets and real biometric, health, or production recordings before sharing evidence.
2. **Make the problem reproducible.** Reduce the report to the smallest deterministic test, sample data, or visual sequence. For a visual defect that cannot yet be automated, write exact manual reproduction steps.
3. **Classify before changing code.** Decide whether the failure is build, test, startup, binding, layout, threading, data-contract, execution, or functional behavior. Inspect one similar successful implementation and its tests.
4. **Fix the smallest coherent cause.** Preserve project boundaries and do not combine unrelated formatting or refactoring with the functional fix. Do not catch, ignore, or convert unexpected exceptions into success states merely to make the symptom disappear.
5. **Lock in the behavior.** Add or update a deterministic regression test where practical. For UI work, verify keyboard access, focus, accessible naming, light/dark themes, and non-color cues in addition to the visual result.
6. **Validate and document.** Run the complete Release validation commands. Update user documentation, localization resources, and an ADR when the change affects product behavior, public compatibility, or architecture.

## Information to include in a Copilot request

A useful request names the user-visible goal, the smallest reproducible case, the expected and actual behavior, affected paths, and acceptance tests. Use this compact template when the cause is unknown:

```text
Diagnose and fix: <one-sentence symptom>

Reproduction:
1. <step>
2. <step>

Expected: <result>
Actual: <result, exception, log, or screenshot>

Constraints: <compatibility, scope, performance, or UI constraints>
Done when: <specific test and/or manual verification>
```

Ask Copilot to investigate and propose the smallest plan before editing when a public contract, Flow project format, data family, or architecture boundary may be affected. For a Block, port, packet, or plugin-contract request, explicitly ask it to load the `dataprocesses-node-development` skill in addition to any desktop guidance.

## Completion record

Every implementation or bug-fix response should state the root cause, changed files, new or updated tests, commands executed, visual verification where applicable, compatibility effect, and remaining limitations. Do not describe incomplete features as implemented. Current pre-alpha limitations are tracked in the root [README](../README.md) and in the initial specification.

## References

[1]: ../global.json "Pinned .NET SDK"
[2]: ../src/DataProcesses.Desktop/DataProcesses.Desktop.csproj "Executable desktop project"
[3]: ../.github/copilot-instructions.md "Repository architecture and data-path instructions"
[4]: ../src/DataProcesses.Desktop/Program.cs "Avalonia desktop entry point"
