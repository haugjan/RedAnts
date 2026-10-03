#!/usr/bin/env bash
set -euo pipefail

REPOSITORY="${REPOSITORY:-haugjan/RedAnts}"
DESTINATION="${DESTINATION:-$HOME/.local/share/redants-gameclock}"
PORT="${PORT:-5080}"
TAG_PREFIX="gameclock-v"

case "$(uname -m)" in
  x86_64|amd64) RID="linux-x64" ;;
  aarch64|arm64) RID="linux-arm64" ;;
  *) echo "Nicht unterstuetzte Architektur: $(uname -m)" >&2; exit 1 ;;
esac

EXE="$DESTINATION/RedAnts.GameClock"
INSTALLED_FILE="$DESTINATION/release.txt"

install_latest() {
  echo "Suche neuestes Matchuhr-Release in $REPOSITORY ..."
  local releases tag latest installed url tmp
  releases="$(curl -fsSL -H 'User-Agent: start-gameclock' "https://api.github.com/repos/$REPOSITORY/releases?per_page=100")"
  tag="$(printf '%s' "$releases" | grep -o "\"tag_name\": *\"$TAG_PREFIX[^\"]*\"" | head -n1 | sed 's/.*"\([^"]*\)"$/\1/')"
  if [ -z "$tag" ]; then echo "Kein Release mit dem Tag-Praefix '$TAG_PREFIX' gefunden." >&2; return 1; fi

  latest="${tag#"$TAG_PREFIX"}"
  installed="$(cat "$INSTALLED_FILE" 2>/dev/null | tr -d '[:space:]' || true)"
  if [ "$installed" = "$latest" ] && [ -x "$EXE" ]; then
    echo "Matchuhr $installed ist aktuell."
    return 0
  fi

  url="https://github.com/$REPOSITORY/releases/download/$tag/gameclock-$latest-$RID.tar.gz"
  tmp="$(mktemp -d)"
  trap 'rm -rf "$tmp"' RETURN
  echo "Lade gameclock-$latest-$RID.tar.gz ..."
  curl -fsSL -o "$tmp/gameclock.tar.gz" "$url"
  pkill -x RedAnts.GameClock 2>/dev/null || true
  mkdir -p "$DESTINATION"
  tar -xzf "$tmp/gameclock.tar.gz" -C "$DESTINATION"
  chmod +x "$EXE"
  echo "Matchuhr $latest installiert nach $DESTINATION."
}

if [ "${1:-}" != "--offline" ]; then
  if ! install_latest; then
    if [ -x "$EXE" ]; then echo "Update nicht moeglich. Starte installierte Version." >&2
    else exit 1; fi
  fi
fi

echo
echo "  Matchuhr: http://localhost:$PORT/"
for ip in $(hostname -I 2>/dev/null || true); do
  case "$ip" in *:*) ;; *) echo "            http://$ip:$PORT/" ;; esac
done
echo

cd "$DESTINATION"
exec "$EXE" --urls "http://0.0.0.0:$PORT"
