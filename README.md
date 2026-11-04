# Üzem-Bácsi CS2 szerver

A szerver saját pluginja (`UzemBacsi`). Ez a repó a szerver gyökérmappáját követi, ezért a Git Pull
a fájlt egyből a helyére teszi:

```
game/csgo/addons/counterstrikesharp/plugins/UzemBacsi/UzemBacsi.dll
```

A repóban csak olyan fájl van, amihez a szerver futás közben nem nyúl, így a Git Pull mindig működik.

## Mit csinál

- **Bhop:** a szóköz nyomva tartásával magától ugrál, de nem gyorsul fel durván (sima bhop, sebességlimittel).
- **Ejtőernyő:** levegőben **E** nyomva tartva egyenletesen ereszkedsz. Elengeded, és rendesen esel tovább.
- **Shotgun tiltás:** a Nova, XM1014, Sawed-Off és MAG-7 nem vásárolható. Ha valahogy mégis megvenné valaki,
  elveszi és visszaadja a pénzét.
- **Hirdetés:** 10 percenként kiírja a chatbe: `[Üzem-Bácsi] Discord: https://discord.gg/T7u8TYJapR`

## Beállítások

Az első indítás után létrejön ez a fájl (ez nincs a repóban, a Git Pull nem írja felül):

```
game/csgo/addons/counterstrikesharp/configs/plugins/UzemBacsi/UzemBacsi.json
```

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

Színek az üzenetekben: `{lime}`, `{lightblue}`, `{red}`, `{default}`, `{green}`, `{gold}`...

Átírás után indítsd újra a szervert.

## Forráskód

A `forras/UzemBacsi` mappában van, a szerver nem használja. Fordítás (.NET 10):

```
dotnet build -c Release -p:CssApi=<counterstrikesharp/api mappa>
```
