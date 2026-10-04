#!/usr/bin/env sh
# Starts the ESP-WebSDR offline bundle on localhost.
set -eu

ROOT=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)

if ! command -v python3 >/dev/null 2>&1; then
  echo "Python 3 is required to start the local server." >&2
  echo "Install Python 3, then run this script again." >&2
  exit 1
fi

exec python3 "$ROOT/tools/local_server.py" "$@"
