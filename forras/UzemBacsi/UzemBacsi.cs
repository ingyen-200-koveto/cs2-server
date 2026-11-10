using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;

namespace UzemBacsi;

public partial class UzemBacsi : BasePlugin, IPluginConfig<UzemConfig>
{
    public override string ModuleName => "UzemBacsi";
    public override string ModuleVersion => "1.5.1";
    public override string ModuleAuthor => "Üzem-Bácsi";

    public UzemConfig Config { get; set; } = new();

    private static readonly Dictionary<string, (int Index, int Price)> Shotguns = new()
    {
        ["weapon_xm1014"] = (25, 2000),
        ["weapon_mag7"] = (27, 1300),
        ["weapon_sawedoff"] = (29, 1100),
        ["weapon_nova"] = (35, 1050),
    };

    private int _nextAd;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _adTimer;

    public void OnConfigParsed(UzemConfig config)
    {
        if (config.Ads.Minutes < 1) config.Ads.Minutes = 1;
        if (config.Parachute.FallSpeed < 10) config.Parachute.FallSpeed = 10;
        Config = config;
    }

    public override void Load(bool hotReload)
    {
        LoadStats();

        RegisterListener<Listeners.OnMapStart>(_ => AddTimer(3.0f, ApplyCvars, TimerFlags.STOP_ON_MAPCHANGE));
        RegisterListener<Listeners.OnMapEnd>(SaveStats);
        RegisterListener<Listeners.OnTick>(ParachuteTick);

        RegisterEventHandler<EventRoundStart>((_, _) => { ResetRoundDamage(); ApplyCvars(); return HookResult.Continue; });
        RegisterEventHandler<EventRoundEnd>((_, _) => { SaveStats(); return HookResult.Continue; });
        RegisterEventHandler<EventItemPurchase>(OnItemPurchase);
        RegisterEventHandler<EventPlayerConnectFull>(OnConnectFull);
        RegisterEventHandler<EventPlayerSpawn>(OnSpawn);
        RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
        RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        RegisterEventHandler<EventPlayerDisconnect>((ev, _) =>
        {
            if (ev.Userid != null && ev.Userid.IsValid) _streak.Remove(ev.Userid.Slot);
            return HookResult.Continue;
        });

        AddCommandListener("say", OnSay);
        AddCommandListener("say_team", OnSayTeam);

        AddCommand("css_dc", "Discord link", (p, _) => { if (p != null) Tell(p, $"Discord: {{lime}}{Config.Discord}"); });
        AddCommand("css_discord", "Discord link", (p, _) => { if (p != null) Tell(p, $"Discord: {{lime}}{Config.Discord}"); });
        AddCommand("css_rs", "Pontszám nullázása", OnResetScore);
        AddCommand("css_rank", "Saját rang", OnRank);
        AddCommand("css_top", "Toplista", OnTop);
        AddCommand("css_def", "Azonnali hatástalanítás", OnDefuse);
        AddCommand("css_norecoil", "Visszarúgás ki/be (admin)", OnNoRecoil);
        RegisterEventHandler<EventBombBegindefuse>(OnBeginDefuse);

        StartAds();
        ApplyCvars();
    }

    public override void Unload(bool hotReload) => SaveStats();

    // ---------- üzenetek ----------

    private void Say(string text) => Server.PrintToChatAll($" {Colors(Config.Prefix)} {Colors(text)}");

    private void Tell(CCSPlayerController p, string text)
    {
        if (p.IsValid) p.PrintToChat($" {Colors(Config.Prefix)} {Colors(text)}");
    }

    private static string Colors(string s)
    {
        foreach (var f in typeof(ChatColors).GetFields())
        {
            var v = f.GetValue(null);
            if (v is not char && v is not string) continue;
            s = s.Replace("{" + f.Name + "}", v.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        return s;
    }

    private void StartAds()
    {
        _adTimer?.Kill();
        if (!Config.Ads.Enabled || Config.Ads.Messages.Count == 0) return;
        _adTimer = AddTimer(Config.Ads.Minutes * 60f, () =>
        {
            if (_nextAd >= Config.Ads.Messages.Count) _nextAd = 0;
            Say(Config.Ads.Messages[_nextAd++]);
        }, TimerFlags.REPEAT);
    }

    // ---------- szerver beállítások ----------

    private static void SetCvar(string name, string value)
    {
        var cvar = ConVar.Find(name);
        if (cvar == null) return;
        cvar.Flags &= ~ConVarFlags.FCVAR_CHEAT;
        Server.ExecuteCommand($"{name} {value}");
    }

    private void ApplyCvars()
    {
        if (Config.Bhop.Enabled)
        {
            SetCvar("sv_autobunnyhopping", "1");
            SetCvar("sv_enablebunnyhopping", Config.Bhop.Unlimited ? "1" : "0");
            SetCvar("sv_staminajumpcost", "0");
            SetCvar("sv_staminalandcost", "0");
        }

        if (Config.MaxPlayers > 0)
            Server.ExecuteCommand($"sv_visiblemaxplayers {Config.MaxPlayers}");

        if (!Config.Bots)
        {
            Server.ExecuteCommand("bot_quota_mode normal");
            Server.ExecuteCommand("bot_quota 0");
            Server.ExecuteCommand("bot_kick");
        }

        if (Config.KeepMap)
        {
            Server.ExecuteCommand("mp_match_end_restart 1");
            Server.ExecuteCommand("mp_match_end_changelevel 0");
        }

        ApplyRecoil();

        var banned = Config.BannedWeapons
            .Where(w => Shotguns.ContainsKey(w))
            .Select(w => Shotguns[w].Index.ToString());
        Server.ExecuteCommand($"mp_items_prohibited \"{string.Join(",", banned)}\"");
    }

    private static bool IsWarmup()
    {
        var proxy = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault();
        return proxy?.GameRules?.WarmupPeriod ?? false;
    }

    private static bool IsHuman(CCSPlayerController? p) => p != null && p.IsValid && !p.IsBot && !p.IsHLTV;

    // ---------- belépés, limit, üdvözlés ----------

    private HookResult OnConnectFull(EventPlayerConnectFull ev, GameEventInfo info)
    {
        var player = ev.Userid;
        if (!IsHuman(player)) return HookResult.Continue;

        if (Config.MaxPlayers > 0)
        {
            int humans = Utilities.GetPlayers().Count(p => IsHuman(p) && p.Connected == PlayerConnectedState.Connected);
            if (humans > Config.MaxPlayers && !AdminManager.PlayerHasPermissions(player, "@css/reservation"))
            {
                var userId = player!.UserId;
                Server.NextFrame(() =>
                    Server.ExecuteCommand($"kickid {userId} \"A szerver tele van (max {Config.MaxPlayers} jatekos).\""));
                return HookResult.Continue;
            }
        }

        TouchStats(player!);

        if (Config.Welcome.Enabled)
        {
            var name = player!.PlayerName;
            AddTimer(3.0f, () =>
            {
                if (player.IsValid) Tell(player, Config.Welcome.Message.Replace("{nev}", name));
            });
        }
        return HookResult.Continue;
    }

    // ---------- shotgun tiltás ----------

    private HookResult OnItemPurchase(EventItemPurchase ev, GameEventInfo info)
    {
        var player = ev.Userid;
        var weapon = ev.Weapon;
        if (player == null || !player.IsValid || string.IsNullOrEmpty(weapon)) return HookResult.Continue;
        if (!weapon.StartsWith("weapon_")) weapon = "weapon_" + weapon;
        if (!Config.BannedWeapons.Contains(weapon)) return HookResult.Continue;

        Server.NextFrame(() =>
        {
            if (!player.IsValid) return;
            player.RemoveItemByDesignerName(weapon);
            var money = player.InGameMoneyServices;
            if (money != null && Shotguns.TryGetValue(weapon, out var s))
            {
                money.Account += s.Price;
                Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices");
            }
            Tell(player, "A shotgunok tiltva vannak ezen a szerveren.");
        });
        return HookResult.Continue;
    }

    // ---------- ejtőernyő ----------

    private void ParachuteTick()
    {
        if (!Config.Parachute.Enabled) return;
        float limit = -Config.Parachute.FallSpeed;

        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || !player.PawnIsAlive) continue;
            if ((player.Buttons & PlayerButtons.Use) == 0) continue;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) continue;
            if ((pawn.Flags & (uint)PlayerFlags.FL_ONGROUND) != 0) continue;
            if (pawn.MoveType == MoveType_t.MOVETYPE_LADDER || pawn.MoveType == MoveType_t.MOVETYPE_NOCLIP) continue;

            var vel = pawn.AbsVelocity;
            if (vel.Z < limit) vel.Z = limit;
        }
    }
}
