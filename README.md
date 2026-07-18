# DataProcesses

**DataProcesses** is an open-source, cross-platform desktop application for building data-processing and signal-analysis flows visually. Users connect typed nodes in a **Flow Editor**, then observe and operate running flows through one or more **Dashboards**.

> **Project status:** Pre-alpha implementation. The repository currently provides data contracts, an Avalonia UI shell, five registered built-in Blocks, and their contract tests. It is not yet ready for production data acquisition: the Flow Editor, dashboard renderer, project persistence, and real Python-worker integration remain incomplete.

## Core concepts

DataProcesses separates block-to-block communication into two explicit paths. **Fast Stream** carries high-rate numeric samples as typed in-memory arrays. **JSON Message** carries events, control messages, and variable payloads in a typed envelope containing a topic, JSON payload, timestamp, and optional correlation ID. CSV such as `millis,data` is a boundary format for import, export, and interoperability; CSV text is not used as the internal high-speed transport.

| Concept | Initial convention |
|---|---|
| Flow Editor | Visual node placement, connection, validation, and execution |
| Dashboard | One of multiple layouts for monitoring and interaction |
| Fast Stream port | Blue circle, `S`, solid connection |
| JSON Message port | Orange diamond, `J`, dashed connection |
| Add-in | A future separately packaged Block provider using the public abstractions assembly |

## MVP direction

The first milestone targets Windows while preserving a cross-platform architecture for future macOS support. The application is written in C#/.NET and uses Avalonia UI. Its initial built-in catalog now contains **Test Signal**, **Low-pass Filter**, **FFT**, **Time Series**, and **Python Output**.

## Initial built-in Blocks

All five Blocks are registered by `BuiltInNodePlugin` and have mirrored contract tests. The table distinguishes the currently executable, contract-level behavior from the Flow Editor and dashboard UI work that remains ahead.

| Block | Typed ports | Current MVP behavior |
|---|---|---|
| **Test Signal** (`dataprocesses.test-signal`) | Fast Stream output: `stream` | Emits one 256-sample, 10 Hz sine-wave frame at a 1 kHz sample rate when started. Runtime configuration and continuous generation are not implemented yet. |
| **Low-pass Filter** (`dataprocesses.filter.low-pass`) | Fast Stream input: `input`; Fast Stream output: `output` | Applies deterministic first-order smoothing independently to each channel and preserves filter state between frames. Cutoff and order settings are not implemented yet. |
| **FFT** (`dataprocesses.analysis.fft`) | Fast Stream input: `input`; Fast Stream `SpectrumFrame` output: `spectrum` | Produces one-sided magnitude spectra and preserves source timing and sequence metadata. The initial calculation prioritizes explicit, deterministic behavior rather than optimized throughput. |
| **Time Series** (`dataprocesses.dashboard.time-series`) | Fast Stream input: `input` | Retains a latest display snapshot, downsampling each channel to at most 512 points. A visual dashboard renderer is not implemented yet. |
| **Python Output** (`dataprocesses.output.python`) | Optional Fast Stream input: `fast-stream`; optional JSON Message input: `message`; optional JSON Message output: `status` | Validates typed input, records a receipt, and emits a deferred-delivery status message. It deliberately does not start or communicate with a Python process yet. |

The full discussion draft is available in [docs/initial-specification.md](docs/initial-specification.md). Architectural decisions are tracked in [docs/decisions](docs/decisions). Local setup, Visual Studio startup, bug diagnosis, and validation are documented in [docs/development-workflow.md](docs/development-workflow.md). The external Add-in model is a documented future direction; its loader, package format, trust model, and distribution workflow are intentionally deferred in [ADR 0003](docs/decisions/0003-future-external-add-ins.md).

## Repository structure

```text
src/
  DataProcesses.Desktop/              Avalonia desktop application
  DataProcesses.Core/                 Flow model and execution-domain logic
  DataProcesses.Plugin.Abstractions/  Stable public contracts for node plugins
  DataProcesses.Nodes.BuiltIn/        Built-in Block catalog and implementations
    Blocks/<BlockName>/               One self-contained directory per Block
tests/
  DataProcesses.Core.Tests/           Unit tests for core behavior
  DataProcesses.Nodes.BuiltIn.Tests/  Mirrored tests for built-in Blocks
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

Each built-in Block has a dedicated directory under [`src/DataProcesses.Nodes.BuiltIn/Blocks`](src/DataProcesses.Nodes.BuiltIn/Blocks). The directory convention and registration steps are defined in the [Built-in Block layout guide](src/DataProcesses.Nodes.BuiltIn/Blocks/README.md) and [ADR 0002](docs/decisions/0002-built-in-block-layout.md).

Repository-wide guidance for GitHub Copilot is maintained in [`.github/copilot-instructions.md`](.github/copilot-instructions.md). Reusable agent skills are stored in [`.github/skills`](.github/skills).

## Contributing

Contributions, design discussions, and early feedback are welcome. Please read [CONTRIBUTING.md](CONTRIBUTING.md), [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md), and [SECURITY.md](SECURITY.md) before participating.

## License

Licensed under the [Apache License 2.0](LICENSE). This license choice can be revisited before the first stable release if the maintainers determine that another license better fits the project.

## 日本語概要

DataProcessesは、高速な時系列データ処理をノード接続で構成し、複数のダッシュボードで可視化・操作するOSSデスクトップアプリケーションです。現在は初期5 Block（Test Signal、Low-pass Filter、FFT、Time Series、Python Output）を実装し、Blockごとの契約テストを備えています。初期版はWindowsを対象としつつ、将来のmacOS対応を妨げない構成を採用します。フローエディター、ダッシュボード描画、プロジェクト保存、実際のPythonプロセス連携は今後の実装範囲です。外部開発者がBlockを提供するアドイン機構は将来方針として記録し、現時点では実装を保留しています。合意済みの詳細仕様は[初期仕様書](docs/initial-specification.md)および[ADR 0003](docs/decisions/0003-future-external-add-ins.md)を参照してください。
