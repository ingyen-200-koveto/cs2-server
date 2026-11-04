using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace UzemBacsi;

public partial class UzemBacsi
{
    private readonly Dictionary<(int Attacker, int Victim), (int Damage, int Hits)> _roundDamage = new();

    private void ResetRoundDamage() => _roundDamage.Clear();

    private HookResult OnPlayerHurt(EventPlayerHurt ev, GameEventInfo info)
    {
        var victim = ev.Userid;
        var attacker = ev.Attacker;
        if (victim == null || !victim.IsValid || attacker == null || !attacker.IsValid) return HookResult.Continue;
        if (attacker == victim) return HookResult.Continue;

        int dmg = ev.DmgHealth;
        if (dmg <= 0) return HookResult.Continue;
        bool head = ev.Hitgroup == 1;

        var key = (attacker.Slot, victim.Slot);
        _roundDamage.TryGetValue(key, out var cur);
        _roundDamage[key] = (cur.Damage + dmg, cur.Hits + 1);

        if (!IsWarmup() && IsHuman(attacker) && attacker.Team != victim.Team)
            AddDamageStat(attacker, dmg);

        if (Config.Damage.HitNumbers && IsHuman(attacker))
        {
            string html = head
                ? $"<font class='fontSize-xl' color='#FFD23F'>{dmg}</font>"
                : $"<font class='fontSize-l' color='#FFFFFF'>{dmg}</font>";
            attacker.PrintToCenterHtml(html);
        }
        return HookResult.Continue;
    }

    private void SendDeathReport(CCSPlayerController victim)
    {
        if (!Config.Damage.DeathReport || !IsHuman(victim)) return;

        var lines = new List<string>();
        foreach (var other in Utilities.GetPlayers())
        {
            if (!other.IsValid || other == victim) continue;
            _roundDamage.TryGetValue((victim.Slot, other.Slot), out var given);
            _roundDamage.TryGetValue((other.Slot, victim.Slot), out var taken);
            if (given.Hits == 0 && taken.Hits == 0) continue;

            lines.Add($"{{default}}{other.PlayerName}: " +
                      $"{{lime}}adtál {given.Damage} ({given.Hits}){{default}} | " +
                      $"{{red}}kaptál {taken.Damage} ({taken.Hits})");
        }

        if (lines.Count == 0) return;
        Tell(victim, "{gold}Sebzés ebben a körben:");
        foreach (var l in lines) victim.PrintToChat($" {Colors(l)}");
    }
}
