# Üzem-Bácsi CS2 szerver

Ez a repó a szerver gyökérmappáját követi (`/home/container`). Benne van minden, ami a CS2 fölé kell:
Metamod, CounterStrikeSharp és a pluginok. Magát a CS2-t nem tartalmazza, azt a szerver induláskor
a Steamről tölti le.

**Fontos: a panelen csak Git Pull-t használj, Git Clone-t soha.** A Clone mindent töröl a szerverről
(a CS2-t is), és csak a repót hagyja meg.

## Mi van benne

| Mappa (`game/csgo/addons/...`) | Mi ez | Verzió |
|---|---|---|
| `metamod` | Metamod:Source | 2.0.0-git1469 |
| `counterstrikesharp` | CounterStrikeSharp, .NET-tel együtt | 1.0.378 |
| `counterstrikesharp/plugins/UzemBacsi` | saját plugin: bhop, ejtőernyő, shotgun tiltás, hirdetés | 1.0.0 |
| `counterstrikesharp/plugins/WeaponPaints` | skinek (`!ws`, `!skins`, `!knife`...), magyar szövegekkel | build-466 |
| `counterstrikesharp/plugins/MenuManagerCore` + `PlayerSettings` + `shared/AnyBaseLib` | a menükhöz kellenek | 1.4.1 / 0.9.4 / 0.9.4 |
| `counterstrikesharp/plugins/CS2-SimpleAdmin` | admin parancsok (`!admin`, `!kick`, `!ban`, `!map`...) | 1.9.1 |

A `configs/core.json`-ban a `FollowCS2ServerGuidelines` ki van kapcsolva, enélkül a skinek nem mennek.

A repóban csak olyan fájl van, amihez a szerver futás közben nem nyúl. A pluginok beállításai
(`configs/plugins/...`) az első indításkor jönnek létre, és a Git Pull nem írja felül őket.

## UzemBacsi

- **Bhop:** a szóköz nyomva tartásával magától ugrál, de nem gyorsul fel durván.
- **Ejtőernyő:** levegőben **E** nyomva tartva egyenletesen ereszkedsz. Elengeded, és rendesen esel tovább.
- **Shotgun tiltás:** a Nova, XM1014, Sawed-Off és MAG-7 nem vásárolható. Ha mégis megvenné valaki,
  elveszi és visszaadja a pénzét.
- **Hirdetés:** 10 percenként: `[Üzem-Bácsi] Discord: https://discord.gg/T7u8TYJapR`

Beállítások: `game/csgo/addons/counterstrikesharp/configs/plugins/UzemBacsi/UzemBacsi.json`

| Beállítás | Alap | Mit jelent |
|---|---|---|
| `Elotag` | `[Üzem-Bácsi]` | az üzenetek eleje a chatben |
| `Bhop.Bekapcsolva` | `true` | bhop be/ki |
| `Bhop.SebessegLimitNelkul` | `false` | `true`-val korlát nélkül gyorsulhatsz (erős bhop) |
| `Ejtoernyo.Bekapcsolva` | `true` | ejtőernyő be/ki |
| `Ejtoernyo.EsesiSebesseg` | `150` | ennél gyorsabban nem esel E-vel; nagyobb szám = gyorsabban ér le |
| `TiltottFegyverek` | a 4 shotgun | ezek nem vásárolhatók |
| `Hirdetes.PercenkentEgyszer` | `10` | ennyi percenként jön hirdetés |
| `Hirdetes.Uzenetek` | Discord link | több sor is lehet, felváltva írja ki őket |

Színek: `{lime}`, `{lightblue}`, `{red}`, `{green}`, `{gold}`, `{default}`...

## Első indítás után egyszer

1. **Metamod ellenőrzése.** A konzolban a `meta list` parancsra a Metamodnak válaszolnia kell. Ha azt írja,
   hogy `Unknown command`, nyisd meg a `game/csgo/gameinfo.gi` fájlt, és a `Game	csgo` sor fölé írd be:
   `Game	csgo/addons/metamod`. (CS2 frissítés után ez néha visszaáll.)
2. **WeaponPaints adatbázis.** A `configs/plugins/WeaponPaints/WeaponPaints.json`-ba írd be az adatbázis
   adatait (host, felhasználó, jelszó, név) és a `"Website"` címét. A jelszó szándékosan nincs a repóban,
   mert a repó nyilvános.
3. **Admin magadnak:** `css_addadmin <SteamID64> <név> @css/root 100`, utána `css_reloadadmins`.

## Forráskód

Az UzemBacsi forrása a `forras/UzemBacsi` mappában van, a szerver nem használja. Fordítás (.NET 10):

```
dotnet build -c Release -p:CssApi=<counterstrikesharp/api mappa>
```
