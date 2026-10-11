#!/usr/bin/env bash
set -euo pipefail

REPO="redants-ch/RedAnts"
HOME_DIR="${XDG_DATA_HOME:-$HOME/.local/share}/redants/show-agent"
EXE="$HOME_DIR/RedAnts.Show.Agent"
RELEASE_FILE="$HOME_DIR/release.txt"
ARCH="$(uname -m)"
case "$ARCH" in
  x86_64) RID="linux-x64" ;;
  aarch64|arm64) RID="linux-arm64" ;;
  *) echo "Unbekannte Architektur $ARCH"; exit 1 ;;
esac

mkdir -p "$HOME_DIR"
if [ "${NO_UPDATE:-0}" != "1" ]; then
  LATEST="$(curl -fsSL -H 'User-Agent: RedAnts-ShowAgent' "https://api.github.com/repos/$REPO/releases?per_page=30" \
    | grep -oE '"tag_name": *"show-agent-v[0-9.]+"' | head -1 | sed -E 's/.*show-agent-v([0-9.]+)"/\1/')" || LATEST=""
  CURRENT="$(cat "$RELEASE_FILE" 2>/dev/null || true)"
  if [ -n "$LATEST" ] && [ "$LATEST" != "$CURRENT" ]; then
    URL="https://github.com/$REPO/releases/download/show-agent-v$LATEST/show-agent-$LATEST-$RID.tar.gz"
    echo "Lade Show-Agent $LATEST ($RID) ..."
    TMP="$(mktemp)"
    if curl -fsSL -o "$TMP" "$URL"; then
      pkill -f "RedAnts.Show.Agent" 2>/dev/null || true
      tar -xzf "$TMP" -C "$HOME_DIR"
      chmod +x "$EXE"
    else
      echo "Download nicht möglich, starte die vorhandene Version."
    fi
    rm -f "$TMP"
  fi
fi

[ -x "$EXE" ] || { echo "Kein Show-Agent unter $HOME_DIR gefunden und kein Download möglich."; exit 1; }
cd "$HOME_DIR"
exec "$EXE" "$@"
