using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;

namespace UzemBacsi;

public partial class UzemBacsi
{
    private bool _voteRunning;
    private readonly Dictionary<ulong, int> _votes = new();
    private readonly HashSet<ulong> _rtv = new();
    private List<MapEntry> _voteMaps = new();
    private DateTime _mapStarted = DateTime.UtcNow;

    private void RegisterVote()
    {
        for (int i = 1; i <= 9; i++)
        {
            int n = i;
            AddCommand($"css_{n}", "Szavazás", (p, _) => CastVote(p, n));
        }
        AddCommand("css_rtv", "Pályaváltás szavazás kérése", OnRtv);
        AddCommand("css_palyak", "Pályák listája", (p, _) => { if (p != null) ShowMaps(p); });
        AddCommand("css_mapvote", "Szavazás indítása (admin)", (p, _) =>
        {
            if (p != null && !AdminManager.PlayerHasPermissions(p, "@css/changemap")
                && !AdminManager.PlayerHasPermissions(p, "@css/root"))
            {
                Tell(p, "Ezt csak admin indíthatja.");
                return;
            }
            StartVote();
        });

        RegisterEventHandler<EventCsWinPanelMatch>((_, _) =>
        {
            if (Config.Vote.Enabled && Config.Vote.AtMatchEnd) StartVote();
            return HookResult.Continue;
        });
        RegisterListener<Listeners.OnMapStart>(_ =>
        {
            _voteRunning = false;
            _votes.Clear();
            _rtv.Clear();
            _mapStarted = DateTime.UtcNow;
        });
    }

    private List<MapEntry> AvailableMaps() =>
        Config.Vote.Maps.Where(m => Server.IsMapValid(m.Map)).Take(9).ToList();

    private void ShowMaps(CCSPlayerController p)
    {
        var maps = AvailableMaps();
        Tell(p, "Pályák: " + string.Join(", ", maps.Select(m => m.Name)) + " | Váltás kérése: {lime}!rtv");
    }

    private void OnRtv(CCSPlayerController? p, CommandInfo cmd)
    {
        if (p == null || !IsHuman(p) || !Config.Vote.Enabled) return;
        if (_voteRunning) { Tell(p, "Már megy egy szavazás, írd be a pálya számát, pl. {lime}!1"); return; }

        var wait = TimeSpan.FromSeconds(Config.Vote.RtvDelaySeconds) - (DateTime.UtcNow - _mapStarted);
        if (wait > TimeSpan.Zero)
        {
            Tell(p, $"Pályaváltást még {(int)Math.Ceiling(wait.TotalSeconds)} másodpercig nem lehet kérni.");
            return;
        }

        _rtv.Add(p.SteamID);
        var humans = Utilities.GetPlayers().Where(IsHuman).Select(x => x.SteamID).ToHashSet();
        _rtv.IntersectWith(humans);
        int need = Math.Max(1, (int)Math.Ceiling(humans.Count * Config.Vote.RtvRatio));

        Say($"{{lime}}{p.PlayerName}{{default}} pályaváltást kér ({_rtv.Count}/{need}). Te is: {{lime}}!rtv");
        if (_rtv.Count >= need) StartVote();
    }

    private void StartVote()
    {
        if (_voteRunning) return;
        _voteMaps = AvailableMaps();
        if (_voteMaps.Count < 2)
        {
            Say("{red}Nincs elég pálya a szerveren a szavazáshoz.");
            return;
        }

        _voteRunning = true;
        _votes.Clear();
        _rtv.Clear();

        Say($"{{gold}}Pályaszavazás!{{default}} Írd be a számot ({Config.Vote.Seconds} mp):");
        var line = string.Join(" ", _voteMaps.Select((m, i) => $"{{lime}}!{i + 1}{{default}} {m.Name}"));
        Server.PrintToChatAll($" {Colors(line)}");

        AddTimer(Config.Vote.Seconds, EndVote, TimerFlags.STOP_ON_MAPCHANGE);
    }

    private void CastVote(CCSPlayerController? p, int n)
    {
        if (p == null || !IsHuman(p)) return;
        if (!_voteRunning) { Tell(p, "Most nincs szavazás. Pályaváltás kérése: {lime}!rtv"); return; }
        if (n < 1 || n > _voteMaps.Count) { Tell(p, $"1 és {_voteMaps.Count} közötti számot írj."); return; }

        _votes[p.SteamID] = n - 1;
        Tell(p, $"Szavazatod: {{lime}}{_voteMaps[n - 1].Name}");
    }

    private void EndVote()
    {
        if (!_voteRunning) return;
        _voteRunning = false;

        if (_votes.Count == 0)
        {
            Say("Senki nem szavazott, marad a pálya.");
            return;
        }

        var counts = _votes.Values.GroupBy(i => i).Select(g => (Index: g.Key, Count: g.Count())).ToList();
        int max = counts.Max(c => c.Count);
        var top = counts.Where(c => c.Count == max).ToList();
        var win = _voteMaps[top[Random.Shared.Next(top.Count)].Index];

        Say($"Nyert: {{gold}}{win.Name}{{default}} ({max} szavazat). Váltás 5 másodperc múlva...");
        AddTimer(5.0f, () => Server.ExecuteCommand($"changelevel {win.Map}"), TimerFlags.STOP_ON_MAPCHANGE);
    }
}
