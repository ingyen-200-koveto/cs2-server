using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

namespace UzemBacsi;

public class PlayerStat
{
    [JsonPropertyName("nev")] public string Name { get; set; } = "";
    [JsonPropertyName("oles")] public int Kills { get; set; }
    [JsonPropertyName("halal")] public int Deaths { get; set; }
    [JsonPropertyName("fejloves")] public int Headshots { get; set; }
    [JsonPropertyName("asszisztencia")] public int Assists { get; set; }
    [JsonPropertyName("sebzes")] public long Damage { get; set; }
}

public partial class UzemBacsi
{
    private Dictionary<ulong, PlayerStat> _stats = new();
    private bool _statsDirty;

    private string StatsPath => Path.GetFullPath(Path.Combine(ModuleDirectory, "..", "..", "configs", "plugins",
        "UzemBacsi", "statisztika.json"));

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    private void LoadStats()
    {
        try
        {
            if (File.Exists(StatsPath))
                _stats = JsonSerializer.Deserialize<Dictionary<ulong, PlayerStat>>(File.ReadAllText(StatsPath)) ?? new();
        }
        catch (Exception e)
        {
            Console.WriteLine($"[UzemBacsi] statisztika.json nem olvasható: {e.Message}");
        }
    }

    private void SaveStats()
    {
        if (!_statsDirty) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(StatsPath)!);
            var tmp = StatsPath + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(_stats, JsonOpts));
            File.Move(tmp, StatsPath, true);
            _statsDirty = false;
        }
        catch (Exception e)
        {
            Console.WriteLine($"[UzemBacsi] statisztika.json nem írható: {e.Message}");
        }
    }

    private PlayerStat? TouchStats(CCSPlayerController p)
    {
        if (!Config.Stats.Enabled || !IsHuman(p) || p.SteamID == 0) return null;
        if (!_stats.TryGetValue(p.SteamID, out var s))
        {
            s = new PlayerStat();
            _stats[p.SteamID] = s;
        }
        s.Name = p.PlayerName;
        _statsDirty = true;
        return s;
    }

    private int Points(PlayerStat s) =>
        s.Kills * Config.Stats.Kill + s.Headshots * Config.Stats.Headshot +
        s.Assists * Config.Stats.Assist + s.Deaths * Config.Stats.Death;

    private RankTier RankOf(int points) =>
        Config.Stats.Ranks.Where(r => points >= r.Points).OrderByDescending(r => r.Points).FirstOrDefault()
        ?? Config.Stats.Ranks.OrderBy(r => r.Points).FirstOrDefault()
        ?? new RankTier { Name = "-" };

    private void AddDamageStat(CCSPlayerController attacker, int dmg)
    {
        var s = TouchStats(attacker);
        if (s != null) s.Damage += dmg;
    }

    private HookResult OnPlayerDeath(EventPlayerDeath ev, GameEventInfo info)
    {
        var victim = ev.Userid;
        if (victim != null && victim.IsValid) Server.NextFrame(() => { if (victim.IsValid) SendDeathReport(victim); });

        if (!Config.Stats.Enabled || IsWarmup()) return HookResult.Continue;

        var attacker = ev.Attacker;
        var assister = ev.Assister;

        var vs = victim != null ? TouchStats(victim) : null;
        if (vs != null) vs.Deaths++;

        bool realKill = attacker != null && attacker.IsValid && victim != null && attacker != victim
                        && attacker.Team != victim.Team;
        if (realKill)
        {
            var a = TouchStats(attacker!);
            if (a != null)
            {
                var before = RankOf(Points(a));
                a.Kills++;
                if (ev.Headshot) a.Headshots++;
                var after = RankOf(Points(a));
                if (after.Points > before.Points)
                    Say($"{{lime}}{attacker!.PlayerName}{{default}} elérte a(z) {{gold}}{after.Name}{{default}} rangot!");
            }
        }

        if (assister != null && assister.IsValid && victim != null && assister.Team != victim.Team)
        {
            var s = TouchStats(assister);
            if (s != null) s.Assists++;
        }
        return HookResult.Continue;
    }

    private void OnRank(CCSPlayerController? player, CommandInfo cmd)
    {
        if (player == null || !IsHuman(player)) return;
        var s = TouchStats(player);
        if (s == null) { Tell(player, "A statisztika ki van kapcsolva."); return; }

        int pts = Points(s);
        var ordered = _stats.OrderByDescending(kv => Points(kv.Value)).Select(kv => kv.Key).ToList();
        int place = ordered.IndexOf(player.SteamID) + 1;
        var rank = RankOf(pts);
        var next = Config.Stats.Ranks.Where(r => r.Points > pts).OrderBy(r => r.Points).FirstOrDefault();

        double kd = s.Deaths == 0 ? s.Kills : (double)s.Kills / s.Deaths;
        int hs = s.Kills == 0 ? 0 : (int)Math.Round(100.0 * s.Headshots / s.Kills);

        Tell(player, $"Rangod: {{gold}}{rank.Name}{{default}} | {{lime}}#{place}{{default}}/{ordered.Count} | {{lime}}{pts}{{default}} pont");
        player.PrintToChat(Colors($" Ölés {{lime}}{s.Kills}{{default}} | Halál {{red}}{s.Deaths}{{default}} | " +
                                  $"K/D {{lime}}{kd:0.00}{{default}} | Fejlövés {{lime}}{hs}%{{default}} | Sebzés {{lime}}{s.Damage}"));
        if (next != null)
            player.PrintToChat(Colors($" Következő rang: {{gold}}{next.Name}{{default}} (még {{lime}}{next.Points - pts}{{default}} pont)"));
    }

    private void OnTop(CCSPlayerController? player, CommandInfo cmd)
    {
        if (player == null || !IsHuman(player)) return;
        var top = _stats.Values.OrderByDescending(Points).Take(5).ToList();
        if (top.Count == 0) { Tell(player, "Még nincs statisztika."); return; }

        Tell(player, "{gold}Toplista:");
        int i = 1;
        foreach (var s in top)
        {
            double kd = s.Deaths == 0 ? s.Kills : (double)s.Kills / s.Deaths;
            player.PrintToChat(Colors($" {{gold}}{i++}.{{default}} {s.Name} - {{lime}}{Points(s)}{{default}} pont " +
                                      $"({RankOf(Points(s)).Name}, K/D {kd:0.00})"));
        }
    }

    private void OnResetScore(CCSPlayerController? player, CommandInfo cmd)
    {
        if (player == null || !player.IsValid) return;

        var stats = player.ActionTrackingServices?.MatchStats;
        if (stats != null)
        {
            stats.Kills = 0;
            stats.Deaths = 0;
            stats.Assists = 0;
            stats.Damage = 0;
            stats.HeadShotKills = 0;
        }
        player.Score = 0;
        player.MVPs = 0;
        Utilities.SetStateChanged(player, "CCSPlayerController", "m_pActionTrackingServices");
        Utilities.SetStateChanged(player, "CCSPlayerController", "m_iScore");
        Utilities.SetStateChanged(player, "CCSPlayerController", "m_iMVPs");

        Tell(player, "Lenulláztad a pontjaidat ebben a meccsben. (A rangod megmarad.)");
    }
}
