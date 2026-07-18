---
name: dataprocesses-desktop-development
description: Implement, test, debug, or review the DataProcesses desktop application outside of a single Block. Use when changing the Avalonia shell, Flow Editor, Dashboard, view models, application startup, desktop composition, or when diagnosing Visual Studio, Avalonia, build, and test failures.
---

# DataProcesses desktop development

Implement desktop features and fixes through the repository's existing boundaries. Use this skill for application and UI work; load `dataprocesses-node-development` as well whenever a request adds or changes a Block, port, packet, or plugin contract.

## Prepare

1. Read `.github/copilot-instructions.md`, `docs/development-workflow.md`, the relevant section of `docs/initial-specification.md`, accepted ADRs, and the nearest path-specific instructions.
2. Inspect the affected view, view model, service, tests, and one comparable implementation before editing.
3. State the smallest reproducible problem or acceptance criteria. Keep unrelated refactoring out of the change.
4. Preserve the boundary that `DataProcesses.Plugin.Abstractions` and `DataProcesses.Core` do not reference `DataProcesses.Desktop` or Avalonia types.

## Run and debug correctly

1. Start the application through `src/DataProcesses.Desktop/DataProcesses.Desktop.csproj`. It is the only current `WinExe` project and must be the Visual Studio startup project.
2. Never attempt to start a class library or test project with F5. Configure `DataProcesses.Desktop` as the single startup project, then reproduce the problem with the debugger attached.
3. For a failure, capture the exact exception, stack trace, inputs, configuration, and observed-versus-expected behavior. Prefer a deterministic automated regression test before or alongside the fix.
4. Distinguish build, test, startup, binding, layout, threading, data-contract, and functional defects before changing code. Do not suppress an exception, warning, analyzer, or failing test merely to make a run appear successful.

## Implement desktop changes

1. Keep presentation state and commands in view models. Limit code-behind to visual behavior that cannot reasonably use bindings or behaviors.
2. Keep graph execution, persistence, data processing, and Block behavior outside views and view models. Add or update Core tests when a UI change exposes domain behavior.
3. Use localization resources for user-visible text. Include keyboard access, focus behavior, accessible names, and non-color state cues in the acceptance criteria.
4. Use responsive layouts, theme resources, and cross-platform Avalonia APIs. Do not introduce Windows-only behavior into shared UI paths.
5. When changing startup or composition, make dependency wiring explicit and keep construction testable.

## Validate and finish

1. Add deterministic synthetic-data tests for behavior changes. Include a reproducible manual verification step for visual behavior when no practical UI test exists.
2. Run the repository commands from the root:

   ```bash
   dotnet restore
   dotnet build --configuration Release
   dotnet test --configuration Release
   ```

3. Update `docs/development-workflow.md`, user-facing documentation, localization resources, and ADRs when the change requires it.
4. Report the problem, root cause, changed files, automated validation, manual visual validation, remaining limitations, and any follow-up work. Do not claim completion if the build or tests fail.
