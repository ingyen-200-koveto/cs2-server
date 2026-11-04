using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace UzemBacsi;

public partial class UzemBacsi
{
    private enum Role { None, Vip, Admin }

    private static Role RoleOf(CCSPlayerController p)
    {
        if (AdminManager.PlayerHasPermissions(p, "@css/generic") || AdminManager.PlayerHasPermissions(p, "@css/root"))
            return Role.Admin;
        if (AdminManager.PlayerHasPermissions(p, "@css/vip") || AdminManager.PlayerHasPermissions(p, "@css/reservation"))
            return Role.Vip;
        return Role.None;
    }

    private HookResult OnSpawn(EventPlayerSpawn ev, GameEventInfo info)
    {
        var p = ev.Userid;
        if (!Config.Tags.Enabled || !IsHuman(p)) return HookResult.Continue;

        Server.NextFrame(() =>
        {
            if (!p!.IsValid) return;
            string clan = RoleOf(p) switch
            {
                Role.Admin => Config.Tags.AdminClan,
                Role.Vip => Config.Tags.VipClan,
                _ => ""
            };
            if (p.Clan == clan) return;
            p.Clan = clan;
            Utilities.SetStateChanged(p, "CCSPlayerController", "m_szClan");
        });
        return HookResult.Continue;
    }

    private HookResult OnSay(CCSPlayerController? p, CommandInfo info) => HandleChat(p, info, false);
    private HookResult OnSayTeam(CCSPlayerController? p, CommandInfo info) => HandleChat(p, info, true);

    private HookResult HandleChat(CCSPlayerController? p, CommandInfo info, bool teamOnly)
    {
        if (!Config.Tags.Enabled || !IsHuman(p)) return HookResult.Continue;

        string msg = info.GetArg(1).Trim();
        if (msg.Length == 0 || msg.StartsWith('!') || msg.StartsWith('/') || msg.StartsWith('.'))
            return HookResult.Continue;

        var role = RoleOf(p!);
        if (role == Role.None) return HookResult.Continue;

        string tag = role == Role.Admin ? Config.Tags.Admin : Config.Tags.Vip;
        char nameColor = p!.Team == CsTeam.CounterTerrorist ? ChatColors.LightBlue
                       : p.Team == CsTeam.Terrorist ? ChatColors.Gold
                       : ChatColors.Grey;
        string dead = p.PawnIsAlive || p.Team == CsTeam.Spectator ? "" : $"{ChatColors.Grey}*HALOTT* ";
        string team = !teamOnly ? "" : p.Team switch
        {
            CsTeam.CounterTerrorist => $"{ChatColors.LightBlue}(CT) ",
            CsTeam.Terrorist => $"{ChatColors.Gold}(T) ",
            _ => $"{ChatColors.Grey}(Néző) "
        };

        string line = $" {dead}{team}{Colors(tag)} {nameColor}{p.PlayerName}{ChatColors.Default}: {msg}";

        foreach (var other in Utilities.GetPlayers())
        {
            if (!IsHuman(other)) continue;
            if (teamOnly && other.Team != p.Team) continue;
            other.PrintToChat(line);
        }
        return HookResult.Handled;
    }
}
