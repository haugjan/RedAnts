#!/usr/bin/env bash
set -euo pipefail

usage() { echo "usage: $0 [--url <adresse>] [--browser <befehl>] [--service] [--once]"; exit 1; }

URL="http://localhost:5150/uhr"
BROWSER=""
SERVICE=0
ONCE=0
while [ $# -gt 0 ]; do
  case "$1" in
    --url) URL="$2"; shift 2 ;;
    --browser) BROWSER="$2"; shift 2 ;;
    --service) SERVICE=1; shift ;;
    --once) ONCE=1; shift ;;
    *) usage ;;
  esac
done

HOME_DIR="${XDG_DATA_HOME:-$HOME/.local/share}/redants/uhr"
PROFILE="$HOME_DIR/profil"
mkdir -p "$PROFILE"

if [ -z "$BROWSER" ]; then
  for candidate in chromium-browser chromium google-chrome google-chrome-stable firefox; do
    if command -v "$candidate" >/dev/null 2>&1; then BROWSER="$candidate"; break; fi
  done
fi
[ -n "$BROWSER" ] || { echo "Kein Browser gefunden. Chromium installieren oder --browser angeben."; exit 1; }

if [ "$SERVICE" = "1" ]; then
  SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
  cp "$SCRIPT_DIR/$(basename "$0")" "$HOME_DIR/start-clock.sh"
  chmod +x "$HOME_DIR/start-clock.sh"
  UNIT_DIR="$HOME/.config/systemd/user"
  mkdir -p "$UNIT_DIR"
  cat > "$UNIT_DIR/redants-uhr.service" <<UNIT
[Unit]
Description=Red Ants Matchuhr
After=graphical-session.target
PartOf=graphical-session.target

[Service]
ExecStart=$HOME_DIR/start-clock.sh --url $URL --browser $BROWSER
Restart=always
RestartSec=5

[Install]
WantedBy=graphical-session.target
UNIT
  systemctl --user daemon-reload
  systemctl --user enable --now redants-uhr.service
  loginctl enable-linger "$USER" >/dev/null 2>&1 || true
  echo "Dienst eingerichtet: redants-uhr.service zeigt $URL"
  echo "Status: systemctl --user status redants-uhr"
  exit 0
fi

if [ -n "${DISPLAY:-}" ] && command -v xset >/dev/null 2>&1; then
  xset s off >/dev/null 2>&1 || true
  xset -dpms >/dev/null 2>&1 || true
  xset s noblank >/dev/null 2>&1 || true
fi
if command -v unclutter >/dev/null 2>&1; then
  pgrep -x unclutter >/dev/null 2>&1 || unclutter -idle 1 >/dev/null 2>&1 &
fi

reachable() {
  curl -fsS -o /dev/null --max-time 2 "$URL" 2>/dev/null
}

show() {
  case "$BROWSER" in
    firefox) "$BROWSER" --kiosk "$URL" ;;
    *) "$BROWSER" --kiosk --noerrdialogs --disable-infobars --disable-session-crashed-bubble \
        --disable-features=TranslateUI --check-for-update-interval=31536000 \
        --autoplay-policy=no-user-gesture-required \
        --user-data-dir="$PROFILE" "$URL" ;;
  esac
}

echo "Matchuhr: $URL im $BROWSER"
while true; do
  waited=0
  until reachable; do
    if [ "$waited" = "0" ]; then echo "Warte auf die Uhr unter $URL ..."; fi
    waited=$((waited + 1))
    sleep 3
  done
  show || true
  [ "$ONCE" = "1" ] && break
  echo "Anzeige beendet, Neustart in 3 Sekunden."
  sleep 3
done
