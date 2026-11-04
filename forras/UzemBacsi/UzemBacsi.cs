using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;

namespace UzemBacsi;

public class BhopConfig
{
    [JsonPropertyName("Bekapcsolva")] public bool Enabled { get; set; } = true;
    [JsonPropertyName("SebessegLimitNelkul")] public bool Unlimited { get; set; } = false;
}

public class ParachuteConfig
{
    [JsonPropertyName("Bekapcsolva")] public bool Enabled { get; set; } = true;
    [JsonPropertyName("EsesiSebesseg")] public float FallSpeed { get; set; } = 150f;
}

public class AdsConfig
{
    [JsonPropertyName("Bekapcsolva")] public bool Enabled { get; set; } = true;
    [JsonPropertyName("PercenkentEgyszer")] public float Minutes { get; set; } = 10f;
    [JsonPropertyName("Uzenetek")] public List<string> Messages { get; set; } = new()
    {
        "Discord: {lime}https://discord.gg/T7u8TYJapR"
    };
}

public class UzemConfig : BasePluginConfig
{
    [JsonPropertyName("Elotag")] public string Prefix { get; set; } = "{lightblue}[Üzem-Bácsi]{default}";
    [JsonPropertyName("Bhop")] public BhopConfig Bhop { get; set; } = new();
    [JsonPropertyName("Ejtoernyo")] public ParachuteConfig Parachute { get; set; } = new();
    [JsonPropertyName("TiltottFegyverek")] public List<string> BannedWeapons { get; set; } = new()
    {
        "weapon_nova", "weapon_xm1014", "weapon_sawedoff", "weapon_mag7"
    };
    [JsonPropertyName("Hirdetes")] public AdsConfig Ads { get; set; } = new();
}

public class UzemBacsi : BasePlugin, IPluginConfig<UzemConfig>
{
    public override string ModuleName => "UzemBacsi";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "Üzem-Bácsi";

    public UzemConfig Config { get; set; } = new();

    private static readonly Dictionary<string, (int Index, int Price)> Shotguns = new()
    {
        ["weapon_xm1014"] = (25, 2000),
        ["weapon_mag7"] = (27, 1300),
        ["weapon_sawedoff"] = (29, 1100),
        ["weapon_nova"] = (35, 1050),
    };

    private const PlayerFlags OnGround = PlayerFlags.FL_ONGROUND;
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
        RegisterListener<Listeners.OnMapStart>(_ => AddTimer(3.0f, ApplyCvars, TimerFlags.STOP_ON_MAPCHANGE));
        RegisterListener<Listeners.OnTick>(ParachuteTick);
        RegisterEventHandler<EventRoundStart>((_, _) => { ApplyCvars(); return HookResult.Continue; });
        RegisterEventHandler<EventItemPurchase>(OnItemPurchase);

        StartAds();
        ApplyCvars();
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

    private void Say(string text)
    {
        Server.PrintToChatAll($" {Colors(Config.Prefix)} {Colors(text)}");
    }

    private static string Colors(string s)
    {
        foreach (var f in typeof(ChatColors).GetFields())
        {
            if (f.GetValue(null) is not char c && f.GetValue(null) is not string) continue;
            s = s.Replace("{" + f.Name + "}", f.GetValue(null)!.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        return s;
    }

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

        var banned = Config.BannedWeapons
            .Where(w => Shotguns.ContainsKey(w))
            .Select(w => Shotguns[w].Index.ToString());
        Server.ExecuteCommand($"mp_items_prohibited \"{string.Join(",", banned)}\"");
    }

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
            player.PrintToChat($" {Colors(Config.Prefix)} A shotgunok tiltva vannak ezen a szerveren.");
        });
        return HookResult.Continue;
    }

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
            if ((pawn.Flags & (uint)OnGround) != 0) continue;
            if (pawn.MoveType == MoveType_t.MOVETYPE_LADDER || pawn.MoveType == MoveType_t.MOVETYPE_NOCLIP) continue;

            var vel = pawn.AbsVelocity;
            if (vel.Z < limit) vel.Z = limit;
        }
    }
}
