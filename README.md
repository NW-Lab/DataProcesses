# DataProcesses

**DataProcesses** is an open-source, cross-platform desktop application for building data-processing and signal-analysis flows visually. Users connect typed nodes in a **Flow Editor**, then observe and operate running flows through one or more **Dashboards**.

> **Project status:** Pre-alpha scaffold. The repository currently defines the architecture, data contracts, first built-in node, tests, and an Avalonia UI shell. It is not yet ready for production data acquisition.

## Core concepts

DataProcesses separates block-to-block communication into two explicit paths. **Fast Stream** carries high-rate numeric samples as typed in-memory arrays. **JSON Message** carries events, control messages, and variable payloads in a versioned envelope. CSV such as `millis,data` is a boundary format for import, export, and interoperability; CSV text is not used as the internal high-speed transport.

| Concept | Initial convention |
|---|---|
| Flow Editor | Visual node placement, connection, validation, and execution |
| Dashboard | One of multiple layouts for monitoring and interaction |
| Fast Stream port | Blue circle, `S`, solid connection |
| JSON Message port | Orange diamond, `J`, dashed connection |
| Plugin | A separately packaged node provider using the public abstractions assembly |

## MVP direction

The first milestone targets Windows while preserving a cross-platform architecture for future macOS support. The application is written in C#/.NET and uses Avalonia UI. Planned MVP nodes are **Test Signal**, **Filter**, **FFT**, **Time Series**, and **Python Output**.

The full discussion draft is available in [docs/initial-specification.md](docs/initial-specification.md). Architectural decisions are tracked in [docs/decisions](docs/decisions).

## Repository structure

```text
src/
  DataProcesses.Desktop/              Avalonia desktop application
  DataProcesses.Core/                 Flow model and execution-domain logic
  DataProcesses.Plugin.Abstractions/  Stable public contracts for node plugins
  DataProcesses.Nodes.BuiltIn/        Built-in node implementations
tests/
  DataProcesses.Core.Tests/           Unit tests
docs/                                 Specifications and decisions
.github/                              Copilot guidance and contribution automation
```

## Prerequisites

Install the .NET SDK version selected by [`global.json`](global.json). The first development platform is Windows, but the source layout is intended to remain portable.

## Build and test

```bash
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
```

Run the desktop shell with:

```bash
dotnet run --project src/DataProcesses.Desktop
```

## Development principles

All connections are typed and validated before execution. Domain and plugin contracts do not depend on UI types. Fast Stream hot paths avoid JSON serialization, CSV parsing, blocking I/O, and unnecessary per-sample allocation. New behavior should arrive with tests and, when it changes an architectural boundary, an Architecture Decision Record.

Repository-wide guidance for GitHub Copilot is maintained in [`.github/copilot-instructions.md`](.github/copilot-instructions.md). Reusable agent skills are stored in [`.github/skills`](.github/skills).

## Contributing

Contributions, design discussions, and early feedback are welcome. Please read [CONTRIBUTING.md](CONTRIBUTING.md), [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md), and [SECURITY.md](SECURITY.md) before participating.

## License

Licensed under the [Apache License 2.0](LICENSE). This license choice can be revisited before the first stable release if the maintainers determine that another license better fits the project.

## 日本語概要

DataProcessesは、高速な時系列データ処理をノード接続で構成し、複数のダッシュボードで可視化・操作するOSSデスクトップアプリケーションです。初期版はWindowsを対象としつつ、将来のmacOS対応を妨げない構成を採用します。合意済みの詳細仕様は[初期仕様書](docs/initial-specification.md)を参照してください。
