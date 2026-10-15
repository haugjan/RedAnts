#!/usr/bin/env bash
set -euo pipefail

usage() { echo "usage: $0 --cloud <url> --hall <id> --key <key> [--deck-port 5150] [--service]"; exit 1; }

CLOUD=""; HALL=""; KEY=""; DECK_PORT=5150; SERVICE=0
while [ $# -gt 0 ]; do
  case "$1" in
    --cloud) CLOUD="$2"; shift 2 ;;
    --hall) HALL="$2"; shift 2 ;;
    --key) KEY="$2"; shift 2 ;;
    --deck-port) DECK_PORT="$2"; shift 2 ;;
    --service) SERVICE=1; shift ;;
    *) usage ;;
  esac
done
[ -n "$CLOUD" ] && [ -n "$HALL" ] && [ -n "$KEY" ] || usage

HOME_DIR="${XDG_DATA_HOME:-$HOME/.local/share}/redants/show-agent"
mkdir -p "$HOME_DIR/data"
HALL_ID="$(echo "$HALL" | tr '[:upper:]' '[:lower:]')"
printf '{\n  "cloudUrl": "%s",\n  "hallId": "%s",\n  "key": "%s",\n  "deckPort": %s\n}\n' "${CLOUD%/}" "$HALL_ID" "$KEY" "$DECK_PORT" > "$HOME_DIR/data/agent.json"
echo "Konfiguration geschrieben: $HOME_DIR/data/agent.json (Halle $HALL_ID, Cloud ${CLOUD%/})"

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
cp "$SCRIPT_DIR/start-show-agent.sh" "$HOME_DIR/start-show-agent.sh" 2>/dev/null || true
chmod +x "$HOME_DIR/start-show-agent.sh"

if [ "$SERVICE" = "1" ]; then
  UNIT_DIR="$HOME/.config/systemd/user"
  mkdir -p "$UNIT_DIR"
  cat > "$UNIT_DIR/redants-show-agent.service" <<UNIT
[Unit]
Description=RedAnts Show-Agent
After=network-online.target

[Service]
ExecStart=$HOME_DIR/start-show-agent.sh
WorkingDirectory=$HOME_DIR
Restart=always
RestartSec=5

[Install]
WantedBy=default.target
UNIT
  systemctl --user daemon-reload
  systemctl --user enable --now redants-show-agent.service
  echo "Dienst redants-show-agent läuft (journalctl --user -u redants-show-agent -f)."
else
  echo "Starte den Agenten (lädt die neuste Version von GitHub) ..."
  exec "$HOME_DIR/start-show-agent.sh"
fi
