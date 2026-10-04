# オフライン・スナップショットの由来

| 項目 | 内容 |
|---|---|
| 上流プロジェクト | [ESPARGOS/esp-web-sdr](https://github.com/ESPARGOS/esp-web-sdr) |
| 取得元 | <https://github.com/ESPARGOS/esp-web-sdr.git> |
| 固定コミット | `70708bafe3bb625a7bb68252afecf54599f54a10` |
| 上流コミット日時 | 2026-10-02T17:29:49+02:00 |
| 上流コミット名 | `Support for ESP32-C2 and ESP32-H2` |
| パッケージ作成日 | 2026-10-04 |
| 上流ライセンス | GPL-3.0-or-later（`LICENSE` を同梱） |

## 同梱した上流コンテンツ

- ESP-WebSDR のブラウザアプリ本体（HTML、CSS、JavaScript、画像、フォント）
- ブラウザ用 ESP-SDR フラッシャーとそのベンダー依存関係
- 上流で同梱されている全 `firmware/` イメージ、マニフェスト、チェックサム、書き込み情報
- 上流のテスト、ドキュメント、ライセンス・サードパーティライセンス

Git の内部メタデータ（`.git/`）だけを除外しています。ビルド済みのみを取り出したパッケージではなく、アプリを変更・再配布できるソース一式を同梱しています。

## このオフライン版で追加したもの

上流のフラッシャーとファームウェアは変更していません。ローカル実行補助に加えて、Wi-Fi チャンネル表示のためにアプリの読込順と測定バーを変更しています。

- `tools/local_server.py` — `127.0.0.1` のみに静的ファイルを配信する Python 3 サーバー
- `start-linux-macos.sh` / `start-windows.bat` — 起動用ラッパー
- `README_OFFLINE_JA.md` / `START_HERE.txt` — 日本語のオフライン手順
- `SHA256SUMS.txt` — パッケージ内容の完全性確認用ハッシュ（作成後に生成）
- `wifi-channels.js` — 2.4 / 5 / 6 GHz の標準 Wi-Fi チャンネル中心周波数の表示用マッピング
- `index.html` / `app.js` — マッピング読込と測定バーの **Wi-Fi CH** 項目を追加
- `tests/wifi-channel.test.mjs` — 周波数とチャンネル番号の対応、スパン要約の回帰テスト

これら追加ファイルは GPL-3.0-or-later の条件で提供します。
