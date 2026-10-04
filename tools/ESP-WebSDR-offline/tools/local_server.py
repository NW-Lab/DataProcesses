#!/usr/bin/env python3
# SPDX-License-Identifier: GPL-3.0-or-later
# Local-only static server for the ESP-WebSDR offline bundle.

from __future__ import annotations

import argparse
import functools
import sys
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path


class LocalOnlyHandler(SimpleHTTPRequestHandler):
    """Serve static bundle files with conservative local-development headers."""

    extensions_map = {
        **SimpleHTTPRequestHandler.extensions_map,
        ".js": "text/javascript; charset=utf-8",
        ".mjs": "text/javascript; charset=utf-8",
        ".json": "application/json; charset=utf-8",
        ".wasm": "application/wasm",
    }

    def end_headers(self) -> None:
        self.send_header("Cache-Control", "no-store")
        self.send_header("X-Content-Type-Options", "nosniff")
        self.send_header("Referrer-Policy", "no-referrer")
        super().end_headers()

    def log_message(self, fmt: str, *args: object) -> None:
        sys.stdout.write("[ESP-WebSDR] " + (fmt % args) + "\n")


def main() -> int:
    bundle_root = Path(__file__).resolve().parent.parent
    parser = argparse.ArgumentParser(
        description="Serve the ESP-WebSDR offline bundle on loopback only."
    )
    parser.add_argument(
        "--port", type=int, default=8765, help="loopback TCP port (default: 8765)"
    )
    parser.add_argument(
        "--host",
        default="127.0.0.1",
        choices=("127.0.0.1", "localhost"),
        help="loopback host only (default: 127.0.0.1)",
    )
    args = parser.parse_args()

    if not 1 <= args.port <= 65535:
        parser.error("--port must be between 1 and 65535")

    handler = functools.partial(LocalOnlyHandler, directory=str(bundle_root))
    ThreadingHTTPServer.allow_reuse_address = True
    try:
        with ThreadingHTTPServer((args.host, args.port), handler) as server:
            print("\nESP-WebSDR offline server is running.")
            print(f"Open: http://localhost:{args.port}/")
            print(f"Flasher: http://localhost:{args.port}/flash.html")
            print("Bound to loopback only; no network connection is used.")
            print("Press Ctrl+C to stop.\n")
            server.serve_forever()
    except OSError as error:
        print(f"Cannot start local server: {error}", file=sys.stderr)
        return 1
    except KeyboardInterrupt:
        print("\nLocal server stopped.")
        return 0


if __name__ == "__main__":
    raise SystemExit(main())
