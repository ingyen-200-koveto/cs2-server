using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace UzemBacsi;

public partial class UzemBacsi
{
    private bool _noRecoil;

    private void ApplyRecoil()
    {
        SetCvar("weapon_recoil_scale", _noRecoil ? "0" : "2");
        SetCvar("weapon_recoil_view_punch_extra", _noRecoil ? "0" : "0.055");
    }

    private void OnNoRecoil(CCSPlayerController? player, CommandInfo cmd)
    {
        if (player != null && !AdminManager.PlayerHasPermissions(player, "@css/generic")
            && !AdminManager.PlayerHasPermissions(player, "@css/root"))
        {
            Tell(player, "Ezt csak admin használhatja.");
            return;
        }
        _noRecoil = !_noRecoil;
        ApplyRecoil();
        var msg = _noRecoil ? "{lime}Visszarúgás KIKAPCSOLVA{default} mindenkinek." : "{red}Visszarúgás visszakapcsolva.";
        foreach (var p in Utilities.GetPlayers())
            if (IsHuman(p) && (AdminManager.PlayerHasPermissions(p, "@css/generic") || AdminManager.PlayerHasPermissions(p, "@css/root")))
                Tell(p, msg);
    }

    private void OnDefuse(CCSPlayerController? player, CommandInfo cmd)
    {
        if (player == null || !player.IsValid || !player.PawnIsAlive) return;
        if (player.Team != CsTeam.CounterTerrorist) { Tell(player, "Csak CT hatástalaníthat."); return; }

        var c4 = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4").FirstOrDefault();
        if (c4 == null || !c4.IsValid || !c4.BombTicking) { Tell(player, "Nincs lerakva bomba."); return; }

        if (Config.Defuse.OnlyWhenNoT &&
            Utilities.GetPlayers().Any(p => p.IsValid && p.PawnIsAlive && p.Team == CsTeam.Terrorist))
        {
            Tell(player, "Csak akkor megy, ha már nincs élő terrorista.");
            return;
        }

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null || c4.AbsOrigin == null) return;
        float dx = pawn.AbsOrigin.X - c4.AbsOrigin.X, dy = pawn.AbsOrigin.Y - c4.AbsOrigin.Y, dz = pawn.AbsOrigin.Z - c4.AbsOrigin.Z;
        if (dx * dx + dy * dy + dz * dz > Config.Defuse.MaxDistance * Config.Defuse.MaxDistance)
        {
            Tell(player, "Menj közelebb a bombához.");
            return;
        }

        c4.BeingDefused = true;
        c4.BombDefuser.Raw = pawn.EntityHandle.Raw;
        c4.DefuseCountDown = Server.CurrentTime;
        Utilities.SetStateChanged(c4, "CPlantedC4", "m_flDefuseCountDown");
        Utilities.SetStateChanged(c4, "CPlantedC4", "m_bBeingDefused");
    }

    private HookResult OnBeginDefuse(EventBombBegindefuse ev, GameEventInfo info)
    {
        if (!Config.Defuse.AutoWhenNoT) return HookResult.Continue;
        if (Utilities.GetPlayers().Any(p => p.IsValid && p.PawnIsAlive && p.Team == CsTeam.Terrorist))
            return HookResult.Continue;

        Server.NextFrame(() =>
        {
            var c4 = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4").FirstOrDefault();
            if (c4 == null || !c4.IsValid || !c4.BeingDefused) return;
            c4.DefuseCountDown = Server.CurrentTime;
            Utilities.SetStateChanged(c4, "CPlantedC4", "m_flDefuseCountDown");
        });
        return HookResult.Continue;
    }
}
