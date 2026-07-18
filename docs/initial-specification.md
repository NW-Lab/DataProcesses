# DataProcesses 初期仕様案

**文書種別:** OSSプロジェクト初期仕様（Discussion Draft）  
**作成者:** Manus AI  
**対象:** DataProcesses v0.1 MVP

## 1. 結論

ご提案の方向性は妥当です。特に、**フロー編集画面とダッシュボードを分離し、ノードをプラグインで追加できる構成**は、テスト信号からフィルター、FFT、表示、外部連携までを段階的に育てる製品に適しています。

リポジトリ名は **`DataProcesses`** で開始して問題ありません。OSSとして公開する場合は **Public repository** とし、ライセンス、コントリビューション手順、プラグインSDKを早い段階から明示します。ただし、英語としては「処理群」という広い意味になるため、製品の説明文には **“Visual data-processing and signal-analysis flow editor”** のような副題を付けることを推奨します。

MVPでは、処理エンジンとGUIをC#で実装し、Windowsを第一対象とします。ただし、将来のmacOS対応を考慮して、UIは **Avalonia UI** を第一候補とします。Avaloniaは共通のコアプロジェクトをWindows、macOS、Linux向けデスクトップで共有できる構成を公式に案内しています。[1]

> **推奨方針:** 「Windows専用として速く作る」のではなく、**Windowsを最初の配布対象にしながら、コード構造は最初からクロスプラットフォームにする**。

## 2. プロジェクトの目的

DataProcessesは、計測データや時系列データを、視覚的なノード接続によって生成、変換、解析、可視化、外部出力できるデスクトップアプリケーションです。利用者はプログラムを書かなくても基本処理を構成でき、開発者は独自ノードを追加できます。

最初の用途はテスト信号処理ですが、将来的には生体信号、センサーデータ、ファイル入力、デバイス入力、ネットワーク入出力、Python解析などへ拡張できる設計とします。

| 項目 | 初期方針 |
|---|---|
| リポジトリ | `DataProcesses` |
| 公開形態 | GitHub Public repository |
| ライセンス候補 | Apache-2.0を第一候補、MITを第二候補 |
| 第一対象OS | Windows 10/11 |
| 将来対象OS | macOS、必要に応じてLinux |
| コア言語 | C# / .NET |
| GUI候補 | Avalonia UI |
| 初期UI言語 | 日本語・英語 |
| 拡張方式 | .NETプラグイン、および将来のプロセス間連携SDK |
| 保存形式 | JSONベースのバージョン付きプロジェクト形式 |

## 3. 用語整理

現在「設定画面」と呼んでいる画面は、一般的な設定ダイアログと混同しやすいため、製品内では **フローエディター（Flow Editor）** と呼ぶことを推奨します。アプリ全体の環境設定は別途 **アプリ設定（Preferences）** とします。

| 用語 | 定義 |
|---|---|
| プロジェクト | 複数のフロー、ダッシュボード、プラグイン参照、実行設定をまとめる保存単位 |
| フロー | ノードと接続線で表現されるデータ処理定義 |
| ノード | 入力、処理、解析、表示、出力のいずれかを担当する拡張可能なブロック |
| ポート | ノード間でデータを受け渡す型付き入出力点 |
| ダッシュボード | 実行中データを表示・操作する画面 |
| ウィジェット | グラフ、数値、状態、操作ボタンなど、ダッシュボード上の部品 |
| Python出力ノード | データを外部Pythonプロセスへ渡すノード |
| Pythonコード出力 | フローをPythonコードとして生成する将来機能。Python出力ノードとは別機能 |

## 4. 画面構成

アプリケーションは、**プロジェクト管理、フロー編集、ダッシュボード、実行監視、アプリ設定**の五領域で構成します。一つのプロジェクトに複数のフローと複数のダッシュボードを保持できるようにします。

| 画面 | 主な役割 | MVP |
|---|---|---|
| ホーム／プロジェクト選択 | 新規作成、最近使ったプロジェクト、ファイルを開く | 必須 |
| フローエディター | ノード配置、接続、プロパティ編集、検証、実行 | 必須 |
| ダッシュボードエディター | ウィジェット配置、サイズ変更、データ割り当て | 必須 |
| ダッシュボード表示 | 実行データの監視、開始・停止、簡易操作 | 必須 |
| 実行ログ／診断 | エラー、警告、ノード状態、処理時間の確認 | 必須 |
| プラグイン管理 | インストール済みプラグインの確認、有効化 | MVPでは読み込みのみ |
| アプリ設定 | 言語、テーマ、既定保存先、ログ設定 | 必須 |

### 4.1 フローエディターのレイアウト

画面左側にノードパレット、中央に無限キャンバス、右側に選択ノードのプロパティ、下部にログと検証結果を配置します。上部には保存、Undo/Redo、検証、実行、停止、ズーム操作を置きます。

複数フローはタブまたはプロジェクトツリーで切り替えます。MVPでは、利用者が同時に編集できるのは一つのアクティブフローとし、複数フローの並列実行は将来機能にします。

### 4.2 ダッシュボードの構造

ダッシュボードは複数作成でき、各ダッシュボードにグリッド形式でウィジェットを配置します。表示ウィジェットはフローノードの出力ポートを参照します。これにより、処理ロジックと表示レイアウトを分離できます。

MVPでは時系列グラフを実装し、将来は数値表示、FFTスペクトル、ゲージ、テーブル、イベントログ、開始・停止ボタンを追加します。

## 5. MVPノードと初期実装状況

Node-REDは、パレットへノードを追加することを主要な拡張方法とし、各ノードを明確で単純な目的に限定する設計指針を示しています。[2] DataProcessesもこの考え方を採用し、巨大な万能ノードではなく、小さな責務のノードを接続します。

現在のpre-alpha実装では、次の5 Blockを`BuiltInNodePlugin`へ登録し、各Blockのポート契約と基本動作をテストしています。表中の「今後のMVP拡張」は製品仕様として維持する目標であり、現時点で実装済みであることを意味しません。

| ノード | 区分 | 入力 | 出力 | 現在のpre-alpha実装 | 今後のMVP拡張 |
|---|---|---|---|---|---|
| Test Signal | Source | なし | `FastStreamFrame` | 開始時に、1 kHz・256サンプルの10 Hz正弦波フレームを1件出力する。 | 波形選択、ホワイトノイズ、周波数・振幅・チャンネル数・ブロック長の設定、連続生成。 |
| Low-pass Filter | Processing | `FastStreamFrame` | `FastStreamFrame` | チャンネルごとに状態を保持する一次平滑化（固定係数0.25）を適用する。 | カットオフ・次数設定、High-passを含む追加フィルター。 |
| FFT | Analysis | `FastStreamFrame` | `SpectrumFrame` | 実数入力の片側振幅スペクトルを計算し、周波数分解能と入力の時刻・シーケンス情報を保持する。 | FFTサイズ、窓関数、オーバーラップ、振幅表現、最適化済みアルゴリズム。 |
| Time Series | Visualization | `FastStreamFrame` | なし | 最新フレームからチャンネルごとに最大512点へ間引いた表示用スナップショットを保持する。 | ダッシュボード描画、表示時間幅、Y軸、色、更新頻度。 |
| Python Output | Output | `FastStreamFrame`またはJSON Message | JSON Messageによる状態 | 入力系統を検証し、受信記録と`deferred`状態メッセージを出力する。Pythonプロセスは起動しない。 | 実行環境、スクリプト、タイムアウト、送信方式、Pythonワーカーとの実通信。 |

## 6. ブロック間データモデル

ブロック間通信は、**高速ストリーム（Fast Stream）**と**JSON payload（Message）**の二系統に分けます。この分離により、連続サンプルを効率よく処理する経路と、設定、イベント、コマンド、結果などを柔軟に扱う経路を明確にできます。

| 系統 | 主用途 | 内部表現 | 代表例 |
|---|---|---|---|
| Fast Stream | 高頻度の連続数値データ | 型付きバイナリ／メモリ上の数値配列 | 波形、センサー値、FFT入力 |
| JSON Message | 低頻度のイベント、制御、可変構造データ | UTF-8 JSON payload | 開始通知、検出イベント、設定変更、解析結果 |

### 6.1 Fast Stream

高速データをブロック間で毎回CSV文字列へ変換すると、文字列生成、数値変換、区切り文字解析の負荷が発生します。そのため、**CSVはファイル保存、コピー、外部連携などの境界形式**として使用し、アプリ内部では数値配列のまま受け渡します。

規則的なサンプリングでは、全サンプルに時刻を付けず、フレーム先頭時刻とサンプル間隔から各サンプル時刻を計算します。これにより、`millis,data`を各行で渡すよりデータ量を抑えられます。

```text
FastStreamFrame
  StartTimeUnixNanoseconds: long
  SamplePeriodNanoseconds: long
  ChannelNames: IReadOnlyList<string>
  Samples: IReadOnlyList<ReadOnlyMemory<double>>  // channel-major
  SequenceNumber: long
```

現在公開している`FastStreamFrame`は、規則的にサンプリングされたチャンネル主順の数値配列を受け渡す最小契約です。`StartTimeUnixNanoseconds`は先頭サンプルの基準時刻、`SamplePeriodNanoseconds`は公称サンプル間隔です。ナノ秒単位の整数を採用するのは、1 ms未満の間隔を表現できるようにするためであり、計測精度が必ずナノ秒になることを意味しません。実際の時刻精度は入力デバイスとOSタイマーに依存します。

`SchemaVersion`、`StreamId`、単位、品質フラグ、メタデータ、不規則サンプリング用の時刻オフセットは、将来の互換性を損なわない形で追加する拡張候補です。現在の最小契約へ暗黙にフィールドを追加するのではなく、必要時にデータ契約と保存形式を明示的に版管理します。

### 6.2 CSV表現

単一チャンネルのCSV入出力は、ご提案の `millis,data` で開始できます。ただし、`millis`が何を基準にするかを固定します。

```csv
millis,data
0,0.0000
1,0.1253
2,0.2487
```

この形式では、`millis`を**フレームまたは記録開始からの相対ミリ秒**とします。絶対時刻が必要なファイルでは、メタデータに開始UTC時刻を記録するか、列名を `timestamp_unix_ms` として意味を明示します。

| 用途 | 推奨CSV形式 |
|---|---|
| 単一チャンネル・相対時刻 | `millis,data` |
| 単一チャンネル・絶対時刻 | `timestamp_unix_ms,data` |
| 複数チャンネル・共通時刻 | `millis,ch1,ch2,...` |
| 不規則時刻または1 ms未満 | `time_ns,data` または十分な桁の秒表現 |

`millis,data`は分かりやすく、1000 Hz以下の表示・保存には使いやすい形式です。ただし、1000 Hzを超えるサンプリングでは複数サンプルが同じミリ秒値になり得るため、内部表現や高精度CSVではナノ秒またはマイクロ秒単位を使用します。

### 6.3 JSON Message

JSON系統は、Node-REDに近い `payload` 中心のメッセージモデルとします。ただし、追跡と互換性のために最小限の共通エンベロープを定義します。

```json
{
  "topic": "event/detected",
  "payload": {
    "type": "peak",
    "value": 0.82,
    "channel": "ch1"
  },
  "timestamp": "2026-07-12T12:34:56.789Z",
  "correlationId": "optional-request-id"
}
```

現在公開している`JsonMessage`は、`topic`、任意のJSON値を持つ`payload`、`timestamp`、任意の`correlationId`から成ります。`payload`はJSONのobject、array、string、number、boolean、nullを許可します。将来、保存・相互運用・メッセージ識別に必要になった時点で、`schemaVersion`、`messageId`、`metadata`などを明示的に追加します。秘密情報や巨大なバイナリデータをJSONへ直接埋め込むことは避けます。

### 6.4 ポートの識別

ポートは**色だけに依存せず、形状とラベルを併用**します。色覚特性、モノクロ表示、テーマ変更でも区別できるようにするためです。

| ポート種別 | 推奨色 | 推奨形状 | 短縮表示 |
|---|---|---|---|
| Fast Stream | 青系 | 円 | `S` |
| JSON Message | 橙系 | ひし形 | `J` |
| 将来の制御専用ポート | 紫系 | 三角形 | `C` |

入力ポートはブロック左側、出力ポートは右側に配置します。マウスオーバー時には、ポート名、データ系統、詳細スキーマ、単位をツールチップ表示します。接続線もポート種別と同じ色・線種を使用し、Fast Streamは実線、JSON Messageは破線とします。

接続時には系統と詳細スキーマを検査し、Fast StreamとJSON Messageの直接接続は拒否します。両者を変換する場合は、`Stream to JSON`や`JSON to Stream`など、変換内容が明示された専用ブロックを使用します。

### 6.5 Spectrumデータ

FFTの結果は高速数値データですが、時間波形とは軸の意味が異なります。MVPではFast Stream系統の派生スキーマとして扱います。

```text
SpectrumFrame
  SourceStartTimeUnixNanoseconds: long
  SourceSamplePeriodNanoseconds: long
  FrequencyResolutionHertz: double
  ChannelNames: IReadOnlyList<string>
  Magnitudes: IReadOnlyList<ReadOnlyMemory<double>>  // channel-major, one-sided
  SequenceNumber: long
```

現在公開している`SpectrumFrame`は、入力フレームの時刻・サンプル周期・シーケンス番号を引き継ぎ、DCをbin 0とする片側振幅スペクトルをチャンネル主順で保持します。各binの間隔は`FrequencyResolutionHertz`です。`SpectrumFrame`も`PortDataKind.FastStream`として扱いますが、時間波形と詳細スキーマが異なります。ポート外観はFast Streamと同じ青系・円形とし、ツールチップとポートラベルで `TimeSeries` と `Spectrum` を区別します。TimeSeries入力へSpectrumを直接つなぐなど、詳細スキーマが異なる接続は拒否します。

## 7. 実行モデル

MVPのフローは **有向非巡回グラフ（DAG）** とし、循環接続は禁止します。実行前に、未接続の必須ポート、型不一致、循環、無効な設定、存在しないプラグインを検証します。

各ノードは入力フレームを非同期に受け取り、出力フレームを発行します。処理が表示より速い場合にメモリが増え続けないよう、接続キューには上限を設けます。キュー超過時の既定動作は、解析系ではエラー、表示系では古いフレームの破棄とし、方針を接続単位で将来設定可能にします。

| 実行状態 | 意味 |
|---|---|
| Stopped | 未実行または停止済み |
| Validating | フロー検証中 |
| Starting | ノード初期化中 |
| Running | 実行中 |
| Stopping | 停止処理中 |
| Faulted | 回復不能なエラーで停止 |

## 8. アドイン仕様（将来方針）

利用者向けの用語として、外部から追加される拡張は**アドイン（Add-in）**と呼びます。現在のコード上の`INodePlugin`などの識別子は、既存契約の安定性を優先して当面維持します。詳細な方針と保留事項は[ADR 0003](decisions/0003-future-external-add-ins.md)を参照してください。

将来、外部開発者は公開SDKのインターフェースを実装した.NETアセンブリとしてBlockを提供できるようにします。.NETには`AssemblyLoadContext`と`AssemblyDependencyResolver`を利用して依存関係を分けて読み込む公式例があります。[3] 同時に、Microsoftの資料は信頼できないコードを同一プロセスへ安全に読み込むことはできないと注意しています。[3]

したがって、将来のアドインは二段階の実行モデルを検討します。ただし、MVPおよび現在のプレアルファ段階では、外部アドインの検出・読み込み・配布機構は実装しません。標準Blockは本体リポジトリの`DataProcesses.Nodes.BuiltIn`で管理します。

| 種別 | 実行場所 | 想定用途 | 導入状態 |
|---|---|---|---|
| Managed Add-in | アプリと同一.NETプロセス | 信頼済みC# Block、高速処理 | 将来検討 |
| Worker Add-in | 別プロセス | Python、他言語、障害分離が必要なBlock | 将来検討 |

現在の公開契約は、概念的に次の要素を含みます。

```csharp
public interface INodeDefinition
{
    string TypeId { get; }
    Version ApiVersion { get; }
    NodeMetadata Metadata { get; }
    IReadOnlyList<PortDefinition> Inputs { get; }
    IReadOnlyList<PortDefinition> Outputs { get; }
    INodeRuntime CreateRuntime(NodeConfiguration configuration);
}

public interface INodeRuntime : IAsyncDisposable
{
    Task StartAsync(NodeContext context, CancellationToken cancellationToken);
    Task OnInputAsync(string portId, IDataFrame frame, CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}
```

将来のアドインでは、マニフェスト（例: `addin.json`）にID、名称、バージョン、対応SDKバージョン、エントリアセンブリ、提供Block一覧、作者、ライセンスを記録することを検討します。正式なスキーマ、配布形式、信頼判断、読み込み方式、互換性ポリシーは未決定です。Block型IDは衝突を避けるため、`org.example.package.block-name`のような逆ドメイン形式を推奨します。

## 9. Python連携

「Pythonへの出力ボード」は、仕様上、次の二つに分離したほうが明確です。

| 機能 | 説明 | 推奨時期 |
|---|---|---|
| Python Outputノード | 実行中データをPythonワーカープロセスへ送り、保存・解析・外部送信を行う | v0.1後半〜v0.2 |
| Python Code Export | フロー全体または一部をPythonコード／Notebookへ変換する | v1.0以降 |

MVPにPython Outputを含める場合、Pythonコードをアプリ本体へ埋め込まず、**別プロセスとして実行**します。初期プロトコルはJSON Linesによる標準入出力とし、メッセージにはスキーマバージョン、フローID、ノードID、フレーム種別を含めます。大容量データで性能不足が判明した段階で、MessagePack、名前付きパイプ、共有メモリなどへ拡張します。

Python側の例外、終了コード、標準エラー、タイムアウトをノード状態としてGUIへ表示します。Python実行を許可する前には、対象スクリプトと実行環境を利用者に明示します。

## 10. 多言語対応

「マルチ言語」は二つの意味があるため、別々に扱います。

| 観点 | MVP方針 |
|---|---|
| UIの多言語化 | 日本語と英語に対応。文字列をコードへ直接記述せず、リソースファイル化する |
| ノード実装言語 | コアと公式ノードはC#。Pythonなどは別プロセスプロトコルで将来対応する |

プロジェクトファイルには表示言語に依存しないIDを保存し、ノード名や説明は言語リソースから表示します。プラグインも言語別リソースを任意で同梱できる仕様にします。

## 11. 保存形式

一つのプロジェクトは、開発中はディレクトリ形式、配布時は必要に応じて単一アーカイブ形式へ移行できるようにします。

```text
MyProject/
  project.json
  flows/
    acquisition.flow.json
    analysis.flow.json
  dashboards/
    monitor.dashboard.json
  scripts/
    output.py
  assets/
```

すべてのJSON文書に `schemaVersion` を持たせます。ファイルにはノードの位置、設定値、ポート接続、ダッシュボード配置、プラグイン要求バージョンを保存します。実行中の一時データや秘密情報は、原則としてプロジェクトファイルへ保存しません。

## 12. 推奨ソリューション構成

```text
DataProcesses.sln
  src/
    DataProcesses.App                 # Avaloniaアプリの起動点
    DataProcesses.UI                  # Views、ViewModels、エディター
    DataProcesses.Core                # データ型、フロー定義、検証
    DataProcesses.Engine              # スケジューラー、実行、キュー
    DataProcesses.Plugin.Abstractions # 公開プラグイン契約
    DataProcesses.Plugin.Loader       # 検出、読み込み、互換性確認
    DataProcesses.Nodes.Core          # Test Signal、Filter、FFT
    DataProcesses.Dashboard           # DashboardとWidget基盤
    DataProcesses.PythonBridge        # 別プロセス連携
  tests/
    DataProcesses.Core.Tests
    DataProcesses.Engine.Tests
    DataProcesses.Nodes.Tests
    DataProcesses.IntegrationTests
  samples/
    SimpleSignalFlow/
    PluginTemplate/
  docs/
    architecture/
    plugin-development/
```

UIと処理エンジンを分離し、エンジンはGUIなしでテストできるようにします。この分離は、将来のCLI実行、ヘッドレス収集、リモート実行にも有効です。

## 13. MVP受け入れ基準

| ID | 受け入れ基準 |
|---|---|
| MVP-01 | Windowsでアプリを起動し、新規プロジェクトを作成・保存・再読込できる |
| MVP-02 | Test Signal、Filter、FFT、Time Seriesをキャンバスへ配置できる |
| MVP-03 | Fast StreamとJSON Messageを色・形状・ラベルで識別でき、型不一致、詳細スキーマ不一致、循環接続を拒否できる |
| MVP-04 | `Test Signal → Filter → Time Series` を開始・停止できる |
| MVP-05 | `Test Signal → FFT` の出力を診断画面またはスペクトル表示で確認できる |
| MVP-06 | 複数フローと複数ダッシュボードを一つのプロジェクトへ保存できる |
| MVP-07 | ノードエラーがアプリ全体のクラッシュではなく、ノード状態とログに反映される |
| MVP-08 | 外部のサンプルC#プラグインを検出し、ノードパレットへ追加できる |
| MVP-09 | UIを日本語と英語で切り替えられる |
| MVP-10 | コア処理、保存互換性、主要ノードに自動テストがある |

## 14. リリース段階

| 段階 | 内容 |
|---|---|
| v0.1 Foundation | プロジェクト保存、フローGUI、実行エンジン、4基本ノード、時系列表示、ログ |
| v0.2 Extensibility | プラグインSDK安定化、Python Worker、プラグインテンプレート、プラグイン管理 |
| v0.3 Dashboard | 複数ウィジェット、FFT表示、レイアウト編集、操作系ウィジェット |
| v0.4 Cross-platform | macOSビルド、パッケージング、OS差異の整理 |
| v1.0 Stable | 保存形式とプラグインAPIの互換性方針、文書化、サンプル、署名済みリリース |

## 15. OSS運営方針

リポジトリ公開時には、`README.md`、`LICENSE`、`CONTRIBUTING.md`、`CODE_OF_CONDUCT.md`、`SECURITY.md`、Issue／Pull Requestテンプレートを用意します。ライセンスは、企業利用とプラグイン開発を促進しつつ特許条項も明示できる **Apache-2.0** を第一候補とします。ただし、より短く単純な条件を優先する場合はMITも適切です。最終決定は、採用予定の依存ライブラリのライセンス確認後に行います。

API安定性は、アプリ本体とプラグインSDKで分けます。v0.x期間中はアプリ内部APIを変更可能とし、公開SDKには互換性レベルと廃止予定を明記します。

## 16. 現時点で決定してよい事項

| 項目 | 推奨決定 |
|---|---|
| リポジトリ名 | `DataProcesses` |
| 公開設定 | Public |
| コア言語 | C# |
| UI | Avalonia UI |
| 初期OS | Windows |
| 将来OS | macOS |
| プロジェクト構造 | 1プロジェクトに複数フロー・複数ダッシュボード |
| 初期ノード | Test Signal、Filter、FFT、Time Series |
| 拡張方式 | C#プラグイン＋将来の別プロセスWorker |
| UI言語 | 日本語・英語を初期から設計対象にする |
| フロー制約 | MVPはDAG、循環禁止 |
| ブロック間通信 | Fast StreamとJSON Messageの二系統 |
| 高速データ内部形式 | 型付き数値配列。CSVは保存・外部連携境界で使用 |
| ポート識別 | 色＋形状＋短縮ラベル＋線種を併用 |

## 17. 次に決めるべき事項

実装開始前に、次の五点だけは合意が必要です。

1. Python Outputをv0.1へ含めるか、v0.2へ送るか。
2. Filterの初期方式をButterworth IIRに固定するか、FIRも含めるか。
3. FFT表示ウィジェットをv0.1に含めるか。FFTノードだけでは結果確認がしにくいため、簡易スペクトル表示はv0.1へ含めることを推奨します。
4. プロジェクト保存をディレクトリ形式にするか、単一ファイル形式にするか。開発初期はGit差分を確認しやすいディレクトリ＋JSON形式を推奨します。
5. OSSライセンスをApache-2.0にするかMITにするか。

以上を前提とすれば、要件は十分に一貫しており、v0.1の設計・実装へ進めます。

## References

[1]: https://docs.avaloniaui.net/docs/app-development/cross-platform-solution-setup "Avalonia Docs — Setting up a cross-platform solution"
[2]: https://nodered.org/docs/creating-nodes/ "Node-RED Documentation — Creating Nodes"
[3]: https://learn.microsoft.com/en-us/dotnet/core/tutorials/creating-app-with-plugin-support "Microsoft Learn — Create a .NET application with plugins"
