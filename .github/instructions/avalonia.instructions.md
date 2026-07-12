---
applyTo: "src/DataProcesses.Desktop/**/*.axaml,src/DataProcesses.Desktop/**/*.axaml.cs"
---

# Avalonia UI instructions

Use MVVM bindings for state and commands. Keep code-behind limited to view-specific interaction that cannot reasonably be represented through bindings or behaviors. Do not put graph execution, persistence, or node business rules in views or view models.

Use localization resource keys for user-visible text. Provide meaningful accessible names, logical keyboard navigation, visible focus states, and non-color cues. Fast Stream and JSON Message ports must remain distinguishable by shape, label, and line style as well as color.

Prefer responsive layouts and theme resources over fixed pixel placement. Verify that layouts tolerate translated text, display scaling, and both light and dark themes. Avoid Windows-only APIs, fonts, paths, and assumptions in shared UI code.

When changing a user-facing view, describe the expected states and include a screenshot or a reproducible visual verification step in the pull request when practical.
