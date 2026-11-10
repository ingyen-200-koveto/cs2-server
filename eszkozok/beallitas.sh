#!/usr/bin/env bash
# Uzem-Bacsi CS2 szerver beallitasa az SRKHost API-n keresztul.
# A VPS-en kell futtatni (ahol a skin weboldal van). Tobbszor is lefuttathato.
#  - WeaponPaints: adatbazis adatok a weboldal config.php-jabol, magyar skinnevek, weboldal link
#  - WeaponPaints: !ws es !skins = jatekon beluli menu, !web = weboldal link
#  - CounterStrikeSharp: a ! parancsok nem latszanak a chatben
#  - SimpleAdmin: az admin parancsok uzeneteit (pl. noclip) csak az adminok latjak
#  - server.cfg: sv_hibernate_when_empty 0
#  - a vegen ujrainditja a szervert
set -euo pipefail

PANEL="https://game.srkhost.eu"
SERVER="8c39b747"
CFG="/var/www/weaponpaints/class/config.php"
WEBSITE="http://79.172.222.68:8080"
WP="/game/csgo/addons/counterstrikesharp/configs/plugins/WeaponPaints/WeaponPaints.json"
CORE="/game/csgo/addons/counterstrikesharp/configs/core.json"
SCFG="/game/csgo/cfg/server.cfg"
SA="/game/csgo/addons/counterstrikesharp/configs/plugins/CS2-SimpleAdmin/CS2-SimpleAdmin.json"

read -rsp "SRKHost API kulcs (ptlc_...): " KEY; echo
[[ -z "$KEY" ]] && { echo "Nem adtal meg kulcsot."; exit 1; }

API="$PANEL/api/client/servers/$SERVER"
H=(-H "Authorization: Bearer $KEY" -H "Accept: application/json")
enc(){ php -r 'echo rawurlencode($argv[1]);' "$1"; }
get(){ curl -sSf "${H[@]}" "$API/files/contents?file=$(enc "$1")"; }
put(){ curl -sSf "${H[@]}" -H "Content-Type: text/plain" -X POST \
        "$API/files/write?file=$(enc "$1")" --data-binary @"$2"; }

TMP="$(mktemp -d)"; trap 'rm -rf "$TMP"' EXIT

# JSON-szerkeszto: a CounterStrikeSharp configok elejen megjegyzes lehet, ezert regex-szel csereljuk az ertekeket.
cat > "$TMP/edit.php" <<'PHP'
<?php
// hasznalat: php edit.php <fajl> <kulcs> <json-ertek> [<kulcs> <json-ertek> ...]
$f = $argv[1];
$s = file_get_contents($f);
for ($i = 2; $i + 1 < count($argv); $i += 2) {
    $k = $argv[$i];
    $v = $argv[$i + 1];
    $re = '/"' . preg_quote($k, '/') . '"\s*:\s*("(?:[^"\\\\]|\\\\.)*"|-?[0-9.]+|true|false|null|\[[^\]]*\])/';
    $n = 0;
    $s = preg_replace_callback($re, fn($m) => '"' . $k . '": ' . $v, $s, 1, $n);
    if ($n === 0) { fwrite(STDERR, "Nem talalom ezt a beallitast: $k\n"); exit(1); }
}
file_put_contents($f, $s);
PHP

js(){ php -r 'echo json_encode($argv[1], JSON_UNESCAPED_UNICODE|JSON_UNESCAPED_SLASHES);' "$1"; }
dbv(){ php -r 'require $argv[1]; echo constant($argv[2]);' "$CFG" "$1"; }

echo "==> WeaponPaints.json"
if ! get "$WP" > "$TMP/wp.json"; then
  echo "Nem sikerult letolteni. Rossz az API kulcs, vagy nincs meg: $WP"; exit 1
fi
php "$TMP/edit.php" "$TMP/wp.json" \
  DatabaseHost "$(js "$(dbv DB_HOST)")" \
  DatabasePort "$(dbv DB_PORT)" \
  DatabaseUser "$(js "$(dbv DB_USER)")" \
  DatabasePassword "$(js "$(dbv DB_PASS)")" \
  DatabaseName "$(js "$(dbv DB_NAME)")" \
  Website "$(js "$WEBSITE")" \
  SkinsLanguage '"hu"' \
  CommandSkin '["web"]' \
  CommandSkinSelection '["ws", "skins"]'
put "$WP" "$TMP/wp.json"
echo "    kesz: !ws = menu, !web = weboldal, adatbazis beirva"

echo "==> core.json (rejtett ! parancsok)"
get "$CORE" > "$TMP/core.json"
php "$TMP/edit.php" "$TMP/core.json" \
  PublicChatTrigger '[]' \
  SilentChatTrigger '["!", "/"]' \
  FollowCS2ServerGuidelines 'false'
put "$CORE" "$TMP/core.json"
echo "    kesz"

echo "==> CS2-SimpleAdmin.json (admin uzenetek csak adminoknak)"
if get "$SA" > "$TMP/sa.json" 2>/dev/null; then
  php "$TMP/edit.php" "$TMP/sa.json" ShowActivityType '3'
  put "$SA" "$TMP/sa.json"
  echo "    kesz"
else
  echo "    nem talalom a SimpleAdmin configot, kihagyom"
fi

echo "==> server.cfg (sv_hibernate_when_empty 0)"
get "$SCFG" > "$TMP/server.cfg" 2>/dev/null || : > "$TMP/server.cfg"
if grep -q "sv_hibernate_when_empty" "$TMP/server.cfg"; then
  sed -i 's/^.*sv_hibernate_when_empty.*$/sv_hibernate_when_empty 0/' "$TMP/server.cfg"
else
  printf '\nsv_hibernate_when_empty 0\n' >> "$TMP/server.cfg"
fi
put "$SCFG" "$TMP/server.cfg"
echo "    kesz"

echo "==> Szerver ujrainditasa"
curl -sSf "${H[@]}" -H "Content-Type: application/json" -X POST "$API/power" -d '{"signal":"restart"}'
echo
echo "KESZ. Varj 1-2 percet, aztan jatekban: !ws"
