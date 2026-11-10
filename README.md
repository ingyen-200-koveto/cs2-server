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
| `counterstrikesharp/plugins/UzemBacsi` | saját plugin: bhop, ejtőernyő, shotgun tiltás, hirdetés, rang, sebzés, chat tagek, !def, !norecoil, injekció sorozatért, pályaszavazás, troll parancsok, max 6 játékos | 1.7.1 |
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
- **Üdvözlés:** belépéskor: `[Üzem-Bácsi] Szia <név>! Skinek: !ws | Rangod: !rank | Discord: !dc`
- **Sebzésszámok:** ha eltalálsz valakit, középen felvillan, mennyit sebeztél (fejlövésnél nagyobb, sárga szám).
- **Sebzés összesítő:** halál után a chatben látod, kinek mennyit adtál és kitől mennyit kaptál abban a körben.
- **Rang és statisztika:** ölés +2, fejlövés +1, asszisztencia +1, halál -1 pont. Rangok: Újonc, Bronz (100),
  Ezüst (300), Arany (600), Platina (1000), Gyémánt (1600), Legenda (2500).
  Az adatok itt vannak: `configs/plugins/UzemBacsi/statisztika.json`.
- **Parancsok:** `!rank` (saját rang), `!top` (top 5), `!rs` (meccsbeli pontok nullázása), `!dc` (Discord link).
  A parancsok nem látszanak a chatben (a `core.json`-ban a `!` csendes trigger).
- **`!def`:** CT-ként a bomba mellett (max 100 egység) bármikor azonnal hatástalanít.
  Ha nincs élő T, az E-vel kezdett hatástalanítás is azonnal kész.
- **Injekció:** 4 ölés halál nélkül = egy injekció (healthshot), aztán minden további 4 után újabb.
- **Pályaszavazás:** Mirage, Dust II, Inferno, Nuke, Ancient, Anubis, Cache. A meccs végén automatikusan indul,
  vagy ha a játékosok 60%-a beírja: `!rtv`. Szavazni a pálya számával lehet: `!1` ... `!7` (20 mp).
  Admin azonnal indíthatja: `!mapvote`. A `!palyak` kiírja a listát. Csak a szerveren meglévő pályák jelennek meg.
- **Troll parancsok (csak admin, az üzenetet csak te látod):** `!troll` (lista), `!rocket`, `!drunk`, `!spin`,
  `!bury` / `!unbury`, `!blind`, `!drop`, `!csirke`, `!chicken` (csirke modell, magadra is: sima `!chicken`), `!lathatatlan`, `!fakeban`.
  Cél: név (részlet is jó), `@all`, `@ct`, `@t`, `@me`. Például: `!rocket pisti`, `!csirke @all`.
- **`!norecoil` (csak admin):** mindenkinek ki/be kapcsolja a fegyverek visszarúgását. Az üzenetet csak az adminok látják.
- **Admin és VIP tag:** a chatben `[ADMIN]` (piros) vagy `[VIP]` (arany) a név előtt, és a pontozótáblán is.
  Admin: aki `@css/generic` vagy `@css/root` jogot kapott. VIP: `@css/vip` vagy `@css/reservation`.
- **Max 6 játékos:** a 7. belépőt kirúgja („A szerver tele van”). Aki `@css/reservation` jogot kapott (VIP), az beléphet.
- **Botok nélkül**, és a meccs végén ugyanazon a pályán indul újra (workshop pályán is).

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
| `Discord` | a Discord link | ezt írja ki a `!dc` |
| `Udvozles.Uzenet` | lásd fent | `{nev}` helyére a játékos neve kerül |
| `Sebzes.SebzesSzamokTalalatkor` | `true` | a felvillanó sebzésszámok be/ki |
| `Sebzes.OsszesitesHalalUtan` | `true` | a halál utáni összesítő be/ki |
| `Cimkek.Admin` / `Cimkek.Vip` | `[ADMIN]` / `[VIP]` | a chat tagek |
| `Statisztika.Rangok` | lásd fent | a rangok neve és ponthatára |
| `Hatastalanitas.CsakHaNincsTerrorista` | `false` | `true`-ra állítva a `!def` csak élő T nélkül megy |
| `Hatastalanitas.AutomatikusHaNincsTerrorista` | `true` | élő T nélkül az E-s hatástalanítás azonnali |
| `Sorozat.OlesekSzama` | `4` | ennyi ölés kell halál nélkül az injekcióhoz |
| `Sorozat.Jutalom` | `weapon_healthshot` | mit kapjon (bármilyen fegyvernév, pl. `weapon_hegrenade`) |
| `Szavazas.Palyak` | a 7 pálya | név + pályafájl (pl. `de_mirage`); workshop pálya nem lehet benne |
| `Szavazas.SzavazasIdeje` | `20` | hány másodpercig lehet szavazni |
| `Szavazas.MeccsVegenSzavazas` | `true` | a meccs végén magától indul a szavazás |
| `Szavazas.RtvArany` | `0.6` | a játékosok hányad része kell az `!rtv`-hez |
| `MaxJatekos` | `6` | ennyi ember lehet fent egyszerre (`0` = nincs limit) |
| `Botok` | `false` | `true`-val maradhatnak a botok |
| `MeccsVegenUgyanazAPalya` | `true` | a meccs végén nem vált pályát |

Színek: `{lime}`, `{lightblue}`, `{red}`, `{green}`, `{gold}`, `{default}`...

## Telepítés és frissítés egy paranccsal

A VPS-en (ahol a skin weboldal van) ez a parancs mindent feltölt és beállít az SRKHost API-n keresztül:
legújabb UzemBacsi, magyar szövegek, WeaponPaints adatbázis és `!ws` menü, rejtett `!` parancsok,
admin üzenetek csak adminoknak, versengő (competitive) játékmód, majd újraindítja a szervert.

```
bash <(curl -sSL https://raw.githubusercontent.com/ingyen-200-koveto/cs2-server/main/eszkozok/beallitas.sh)
```

Kell hozzá egy SRKHost API kulcs (panel, bal oldalt **API**).

## Első indítás után egyszer

1. **Metamod ellenőrzése.** A konzolban a `meta list` parancsra a Metamodnak válaszolnia kell. Ha azt írja,
   hogy `Unknown command`, nyisd meg a `game/csgo/gameinfo.gi` fájlt, és a `Game	csgo` sor fölé írd be:
   `Game	csgo/addons/metamod`. (CS2 frissítés után ez néha visszaáll.)
2. **WeaponPaints adatbázis.** A `configs/plugins/WeaponPaints/WeaponPaints.json`-ba írd be az adatbázis
   adatait (host, felhasználó, jelszó, név) és a `"Website"` címét. A jelszó szándékosan nincs a repóban,
   mert a repó nyilvános.
3. **Admin magadnak:** `css_addadmin <SteamID64> <név> @css/root 100`, utána `css_reloadadmins`.
   VIP-et így adsz: `css_addadmin <SteamID64> <név> @css/vip 10`.

## Forráskód

Az UzemBacsi forrása a `forras/UzemBacsi` mappában van, a szerver nem használja. Fordítás (.NET 10):

```
dotnet build -c Release -p:CssApi=<counterstrikesharp/api mappa>
```
