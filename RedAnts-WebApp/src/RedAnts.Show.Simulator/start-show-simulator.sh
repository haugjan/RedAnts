#!/usr/bin/env bash
set -euo pipefail

REPO="haugjan/RedAnts"
PORT="${1:-5090}"
HOME_DIR="${XDG_DATA_HOME:-$HOME/.local/share}/redants/show-simulator"
EXE="$HOME_DIR/RedAnts.Show.Simulator"
RELEASE_FILE="$HOME_DIR/release.txt"
case "$(uname -m)" in
  x86_64) RID="linux-x64" ;;
  aarch64|arm64) RID="linux-arm64" ;;
  *) echo "Unbekannte Architektur $(uname -m)"; exit 1 ;;
esac

mkdir -p "$HOME_DIR"
if [ "${NO_UPDATE:-0}" != "1" ]; then
  LATEST="$(curl -fsSL -H 'User-Agent: RedAnts-ShowSimulator' "https://api.github.com/repos/$REPO/releases?per_page=30" \
    | grep -oE '"tag_name": *"show-simulator-v[0-9.]+"' | head -1 | sed -E 's/.*show-simulator-v([0-9.]+)"/\1/')" || LATEST=""
  CURRENT="$(cat "$RELEASE_FILE" 2>/dev/null || true)"
  if [ -n "$LATEST" ] && [ "$LATEST" != "$CURRENT" ]; then
    URL="https://github.com/$REPO/releases/download/show-simulator-v$LATEST/show-simulator-$LATEST-$RID.tar.gz"
    echo "Lade Show-Simulator $LATEST ($RID) ..."
    TMP="$(mktemp)"
    if curl -fsSL -o "$TMP" "$URL"; then
      pkill -f "RedAnts.Show.Simulator" 2>/dev/null || true
      tar -xzf "$TMP" -C "$HOME_DIR"
      chmod +x "$EXE"
    else
      echo "Download nicht möglich, starte die vorhandene Version."
    fi
    rm -f "$TMP"
  fi
fi

[ -x "$EXE" ] || { echo "Kein Show-Simulator unter $HOME_DIR gefunden und kein Download möglich."; exit 1; }
cd "$HOME_DIR"
echo "Show-Simulator: http://localhost:$PORT"
exec "$EXE" --urls "http://localhost:$PORT"
