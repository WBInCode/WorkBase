#!/usr/bin/env bash
# Wlacza skanowanie antywirusowe wgrywanych plikow w WorkBase.
# Korzystamy z dzialajacego juz clamd (kontener wb-chat-clamav w sieci wb-net),
# zeby nie stawiac drugiego procesu z wlasna baza sygnatur i ~1 GB pamieci.
set -euo pipefail

ENV_FILE=/opt/wb/workbase/.env
TS=$(date +%Y%m%d-%H%M%S)

if grep -q '^ClamAv__Enabled=' "$ENV_FILE"; then
  echo "Konfiguracja skanera juz istnieje — nic nie zmieniam."
  grep '^ClamAv__' "$ENV_FILE"
  exit 0
fi

cp "$ENV_FILE" "$ENV_FILE.bak-clamav-$TS"
echo "kopia: $ENV_FILE.bak-clamav-$TS"

cat >> "$ENV_FILE" <<'KONIEC'

# Skanowanie antywirusowe wgrywanych plikow (wspoldzielony clamd chatu).
ClamAv__Enabled=true
ClamAv__Host=wb-chat-clamav
ClamAv__Port=3310
ClamAv__TimeoutSeconds=60
ClamAv__AllowUploadWhenScannerUnavailable=false
KONIEC

echo "dopisano:"
grep '^ClamAv__' "$ENV_FILE"
