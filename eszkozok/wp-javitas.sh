#!/usr/bin/env bash
# WeaponPaints adatbazis-jelszo + sv_hibernate_when_empty javitas az SRKHost API-n keresztul.
# A VPS-en kell futtatni (ahol a skin weboldal van).
set -euo pipefail

PANEL="https://game.srkhost.eu"
SERVER="8c39b747"
CFG="/var/www/weaponpaints/class/config.php"
WP="/game/csgo/addons/counterstrikesharp/configs/plugins/WeaponPaints/WeaponPaints.json"
SCFG="/game/csgo/cfg/server.cfg"

read -rsp "SRKHost API kulcs (ptlc_...): " KEY; echo
[[ -z "$KEY" ]] && { echo "Nem adtal meg kulcsot."; exit 1; }

API="$PANEL/api/client/servers/$SERVER"
H=(-H "Authorization: Bearer $KEY" -H "Accept: application/json")
enc(){ php -r 'echo rawurlencode($argv[1]);' "$1"; }

get(){ curl -sSf "${H[@]}" "$API/files/contents?file=$(enc "$1")"; }
put(){ curl -sSf "${H[@]}" -H "Content-Type: text/plain" -X POST \
        "$API/files/write?file=$(enc "$1")" --data-binary @"$2"; }

TMP="$(mktemp -d)"; trap 'rm -rf "$TMP"' EXIT

echo "==> WeaponPaints.json letoltese"
if ! get "$WP" > "$TMP/wp.json"; then
  echo "Nem sikerult. Rossz az API kulcs, vagy nincs meg a fajl: $WP"; exit 1
fi

echo "==> Adatbazis adatok beirasa (a weboldal config.php-jabol)"
php -r '
  require $argv[1];
  $s = file_get_contents($argv[2]);
  $set = function($k, $v, $quote = true) use (&$s) {
    $val = $quote ? json_encode($v, JSON_UNESCAPED_UNICODE|JSON_UNESCAPED_SLASHES) : $v;
    $re = "/\"" . preg_quote($k, "/") . "\"\\s*:\\s*(\"(?:[^\"\\\\]|\\\\.)*\"|-?[0-9]+)/";
    $n = 0;
    $s = preg_replace_callback($re, fn($m) => "\"$k\": " . $val, $s, 1, $n);
    if ($n === 0) { fwrite(STDERR, "Nem talalom: $k\n"); exit(1); }
  };
  $set("DatabaseHost", DB_HOST);
  $set("DatabasePort", (string)(int)DB_PORT, false);
  $set("DatabaseUser", DB_USER);
  $set("DatabasePassword", DB_PASS);
  $set("DatabaseName", DB_NAME);
  $set("Website", "http://79.172.222.68:8080");
  $set("SkinsLanguage", "hu");
  file_put_contents($argv[2], $s);
' "$CFG" "$TMP/wp.json"

put "$WP" "$TMP/wp.json"
echo "    kesz."

echo "==> server.cfg: sv_hibernate_when_empty 0"
get "$SCFG" > "$TMP/server.cfg" 2>/dev/null || : > "$TMP/server.cfg"
if grep -q "sv_hibernate_when_empty" "$TMP/server.cfg"; then
  sed -i 's/^.*sv_hibernate_when_empty.*$/sv_hibernate_when_empty 0/' "$TMP/server.cfg"
else
  printf '\nsv_hibernate_when_empty 0\n' >> "$TMP/server.cfg"
fi
put "$SCFG" "$TMP/server.cfg"
echo "    kesz."

echo "==> Szerver ujrainditasa"
curl -sSf "${H[@]}" -H "Content-Type: application/json" -X POST "$API/power" -d '{"signal":"restart"}'
echo
echo "KESZ. Varj 1-2 percet, aztan nezd meg a konzolban, hogy eltunt-e a WeaponPaints hiba."
