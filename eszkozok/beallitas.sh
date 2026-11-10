#!/usr/bin/env bash
# Uzem-Bacsi CS2 szerver: telepites + beallitas az SRKHost API-n keresztul.
# A VPS-en kell futtatni (ahol a skin weboldal van). Tobbszor is lefuttathato.
#  - feltolti a legfrissebb UzemBacsi.dll-t a GitHubrol
#  - feltolti a magyar szovegeket (WeaponPaints, SimpleAdmin)
#  - WeaponPaints: adatbazis a weboldal config.php-jabol, magyar skinnevek, !ws = menu, !web = weboldal
#  - CounterStrikeSharp: a ! parancsok nem latszanak a chatben
#  - SimpleAdmin: az admin parancsok uzeneteit csak az adminok latjak
#  - UzemBacsi: !def barmikor mukodik
#  - server.cfg: sv_hibernate_when_empty 0
#  - jatekmod: versengo (competitive) az inditasi parameterekben
#  - a vegen ujrainditja a szervert
set -euo pipefail

PANEL="https://game.srkhost.eu"
SERVER="8c39b747"
REPO="https://raw.githubusercontent.com/ingyen-200-koveto/cs2-server/main"
CFG="/var/www/weaponpaints/class/config.php"
WEBSITE="http://79.172.222.68:8080"

CSS="/game/csgo/addons/counterstrikesharp"
WP="$CSS/configs/plugins/WeaponPaints/WeaponPaints.json"
CORE="$CSS/configs/core.json"
SA="$CSS/configs/plugins/CS2-SimpleAdmin/CS2-SimpleAdmin.json"
UB="$CSS/configs/plugins/UzemBacsi/UzemBacsi.json"
SCFG="/game/csgo/cfg/server.cfg"

LANGS=(
  "plugins/WeaponPaints/lang/hu.json"
  "plugins/WeaponPaints/lang/en.json"
  "plugins/CS2-SimpleAdmin/lang/hu.json"
  "plugins/CS2-SimpleAdmin/lang/en.json"
  "plugins/CS2-SimpleAdmin_FunCommands/lang/hu.json"
  "plugins/CS2-SimpleAdmin_FunCommands/lang/en.json"
)

read -rsp "SRKHost API kulcs (ptlc_...): " KEY; echo
[[ -z "$KEY" ]] && { echo "Nem adtal meg kulcsot."; exit 1; }

API="$PANEL/api/client/servers/$SERVER"
H=(-H "Authorization: Bearer $KEY" -H "Accept: application/json")
enc(){ php -r 'echo rawurlencode($argv[1]);' "$1"; }
get(){ curl -sSf "${H[@]}" "$API/files/contents?file=$(enc "$1")"; }
put(){ curl -sSf "${H[@]}" -H "Content-Type: text/plain" -X POST \
        "$API/files/write?file=$(enc "$1")" --data-binary @"$2" >/dev/null; }
mkdirp(){ # mkdirp <szulo mappa> <uj mappa neve>
  curl -sS "${H[@]}" -H "Content-Type: application/json" -X POST "$API/files/create-folder" \
       -d "{\"root\":\"$1\",\"name\":\"$2\"}" >/dev/null || true; }
upload(){ # upload <helyi fajl> <cel mappa>
  local url
  url="$(curl -sSf "${H[@]}" "$API/files/upload" | php -r '$j=json_decode(stream_get_contents(STDIN),true); echo $j["attributes"]["url"] ?? "";')"
  [[ -z "$url" ]] && { echo "Nem kaptam feltoltesi cimet."; return 1; }
  curl -sSf -F "files=@$1" "$url&directory=$(enc "$2")" >/dev/null
}

TMP="$(mktemp -d)"; trap 'rm -rf "$TMP"' EXIT

echo "==> API kulcs ellenorzese"
if ! curl -sSf "${H[@]}" "$API" >/dev/null; then
  echo "Az API kulcs nem jo, vagy nem ehhez a szerverhez tartozik."; exit 1
fi

echo "==> UzemBacsi.dll feltoltese"
curl -sSfL -o "$TMP/UzemBacsi.dll" "$REPO/game/csgo/addons/counterstrikesharp/plugins/UzemBacsi/UzemBacsi.dll"
mkdirp "$CSS/plugins" "UzemBacsi"
upload "$TMP/UzemBacsi.dll" "$CSS/plugins/UzemBacsi"
echo "    kesz"

echo "==> Magyar szovegek feltoltese"
for f in "${LANGS[@]}"; do
  curl -sSfL -o "$TMP/lang.json" "$REPO/game/csgo/addons/counterstrikesharp/$f"
  put "$CSS/$f" "$TMP/lang.json"
  echo "    $f"
done

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
if get "$WP" > "$TMP/wp.json" 2>/dev/null; then
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
else
  echo "    nincs meg (a szerver elso inditasa utan jon letre), kihagyom"
fi

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
  echo "    nincs meg, kihagyom"
fi

echo "==> UzemBacsi.json (!def barmikor)"
if get "$UB" > "$TMP/ub.json" 2>/dev/null; then
  php "$TMP/edit.php" "$TMP/ub.json" CsakHaNincsTerrorista 'false' || true
  put "$UB" "$TMP/ub.json"
  echo "    kesz"
else
  echo "    meg nincs (az uj alapbeallitas mar ez), kihagyom"
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

echo "==> Jatekmod: versengo (competitive)"
curl -sSf "${H[@]}" "$API/startup" > "$TMP/startup.json"
cat > "$TMP/mode.php" <<'PHP'
<?php
// kiirja: <env_variable>\t<uj ertek>  vagy hibauzenetet
$j = json_decode(file_get_contents($argv[1]), true);
$vars = array_map(fn($d) => $d["attributes"], $j["data"] ?? []);
$pick = null;
foreach ($vars as $v) if (str_contains((string)$v["server_value"], "game_alias")) { $pick = $v; break; }
if (!$pick) foreach ($vars as $v) if (str_contains((string)$v["server_value"], "+map")) { $pick = $v; break; }
if (!$pick) { fwrite(STDERR, "Nem talalom az inditasi parancs mezot.\n"); exit(1); }
if (empty($pick["is_editable"])) { fwrite(STDERR, "Az inditasi parancs mezo nem szerkesztheto.\n"); exit(1); }
$val = (string)$pick["server_value"];
if (preg_match('/\+game_alias\s+\S+/', $val)) $val = preg_replace('/\+game_alias\s+\S+/', '+game_alias competitive', $val);
else $val = trim($val) . " +game_alias competitive";
echo $pick["env_variable"], "\t", $val;
PHP
if MODE="$(php "$TMP/mode.php" "$TMP/startup.json")"; then
  VAR="${MODE%%$'\t'*}"; VAL="${MODE#*$'\t'}"
  BODY="$(php -r 'echo json_encode(["key"=>$argv[1],"value"=>$argv[2]], JSON_UNESCAPED_SLASHES);' "$VAR" "$VAL")"
  curl -sSf "${H[@]}" -H "Content-Type: application/json" -X PUT "$API/startup/variable" -d "$BODY" >/dev/null
  echo "    kesz: $VAL"
else
  echo "    nem sikerult, ird at kezzel az Inditasi parameterekben: +game_alias competitive"
fi

echo "==> Szerver ujrainditasa"
curl -sSf "${H[@]}" -H "Content-Type: application/json" -X POST "$API/power" -d '{"signal":"restart"}' >/dev/null
echo
echo "KESZ. Varj 1-2 percet, aztan jatekban: !ws, !def, !rank"
