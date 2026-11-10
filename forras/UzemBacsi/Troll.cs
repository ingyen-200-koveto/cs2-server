using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;

namespace UzemBacsi;

public partial class UzemBacsi
{
    private readonly HashSet<int> _invisible = new();

    private void RegisterTroll()
    {
        AddCommand("css_troll", "Troll parancsok listája", (p, _) =>
        {
            if (!IsTrollAdmin(p)) return;
            Tell(p!, "{gold}Troll parancsok:{default} !rocket, !drunk, !spin, !bury, !unbury, !blind, !drop, !csirke, !lathatatlan, !fakeban");
            p!.PrintToChat(Colors(" Cél: név (részlet is jó), {lime}@all{default}, {lime}@ct{default}, {lime}@t{default}, {lime}@me"));
        });

        Troll("css_rocket", "Kilövi a magasba, aztán felrobban", (p, t) =>
        {
            if (!t.PawnIsAlive) return;
            var pawn = t.PlayerPawn.Value!;
            pawn.Teleport(null, null, new Vector(0, 0, 1100));
            AddTimer(1.4f, () => { if (t.IsValid && t.PawnIsAlive) t.PlayerPawn.Value?.CommitSuicide(true, true); });
        });

        Troll("css_drunk", "10 mp-ig részeg, inog a kamera", (p, t) =>
        {
            int ticks = 0;
            var timer = AddTimer(0.1f, () =>
            {
                if (!t.IsValid || !t.PawnIsAlive) return;
                ticks++;
                var pawn = t.PlayerPawn.Value!;
                var a = pawn.EyeAngles;
                pawn.Teleport(null, new QAngle(a.X + Random.Shared.Next(-6, 7), a.Y + Random.Shared.Next(-12, 13),
                    (float)(Math.Sin(ticks * 0.4) * 15)), null);
            }, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
            KillAfter(timer, 10f, () =>
            {
                if (t.IsValid && t.PawnIsAlive)
                {
                    var a = t.PlayerPawn.Value!.EyeAngles;
                    t.PlayerPawn.Value!.Teleport(null, new QAngle(a.X, a.Y, 0), null);
                }
            });
        });

        Troll("css_spin", "10 mp-ig pörög", (p, t) =>
        {
            var timer = AddTimer(0.05f, () =>
            {
                if (!t.IsValid || !t.PawnIsAlive) return;
                var pawn = t.PlayerPawn.Value!;
                var a = pawn.EyeAngles;
                pawn.Teleport(null, new QAngle(a.X, a.Y + 25, 0), null);
            }, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
            KillAfter(timer, 10f);
        });

        Troll("css_bury", "Beássa a földbe", (p, t) => MoveZ(t, -45));
        Troll("css_unbury", "Kiássa a földből", (p, t) => MoveZ(t, 50));

        Troll("css_blind", "5 mp-re megvakítja", (p, t) =>
        {
            if (!t.PawnIsAlive) return;
            var pawn = t.PlayerPawn.Value!;
            pawn.FlashMaxAlpha = 255;
            pawn.FlashDuration = 5f;
            pawn.BlindStartTime = Server.CurrentTime;
            pawn.BlindUntilTime = Server.CurrentTime + 5f;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawnBase", "m_flFlashMaxAlpha");
            Utilities.SetStateChanged(pawn, "CCSPlayerPawnBase", "m_flFlashDuration");
            Utilities.SetStateChanged(pawn, "CCSPlayerPawnBase", "m_blindStartTime");
            Utilities.SetStateChanged(pawn, "CCSPlayerPawnBase", "m_blindUntilTime");
        });

        Troll("css_drop", "Kiejti a kezéből a fegyvert", (p, t) =>
        {
            if (t.PawnIsAlive) t.DropActiveWeapon();
        });

        Troll("css_csirke", "Csirkéket rak köré", (p, t) =>
        {
            var pos = t.PlayerPawn.Value?.AbsOrigin;
            if (pos == null) return;
            for (int i = 0; i < 6; i++)
            {
                var c = Utilities.CreateEntityByName<CChicken>("chicken");
                if (c == null) continue;
                double ang = i * Math.PI / 3;
                c.Teleport(new Vector(pos.X + (float)Math.Cos(ang) * 60, pos.Y + (float)Math.Sin(ang) * 60, pos.Z + 10), null, null);
                c.DispatchSpawn();
            }
        });

        Troll("css_lathatatlan", "Láthatatlanná teszi / visszaállítja", (p, t) =>
        {
            if (!t.PawnIsAlive) return;
            var pawn = t.PlayerPawn.Value!;
            bool hide = _invisible.Add(t.Slot);
            if (!hide) _invisible.Remove(t.Slot);
            pawn.RenderMode = RenderMode_t.kRenderTransAlpha;
            pawn.Render = Color.FromArgb(hide ? 0 : 255, 255, 255, 255);
            Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
        });

        Troll("css_fakeban", "Kamu ban üzenet mindenkinek, aztán kiderül, hogy vicc", (p, t) =>
        {
            Server.PrintToChatAll(Colors($" {{lightblue}}[Üzem-Bácsi]{{default}} {{lightred}}Admin{{default}} végleg kitiltotta {{lightred}}{t.PlayerName}{{default}} játékost. Indok: {{lightred}}csalás"));
            var name = t.PlayerName;
            AddTimer(4f, () => Say($"Nyugi {{lime}}{name}{{default}}, csak vicc volt :D"));
        });

        RegisterEventHandler<EventRoundStart>((_, _) => { _invisible.Clear(); return HookResult.Continue; });
    }

    private bool IsTrollAdmin(CCSPlayerController? p)
    {
        if (p == null) return true;
        if (AdminManager.PlayerHasPermissions(p, "@css/generic") || AdminManager.PlayerHasPermissions(p, "@css/root"))
            return true;
        Tell(p, "Ezt csak admin használhatja.");
        return false;
    }

    private void Troll(string name, string desc, Action<CCSPlayerController?, CCSPlayerController> act)
    {
        AddCommand(name, desc, (p, cmd) =>
        {
            if (!IsTrollAdmin(p)) return;
            if (cmd.ArgCount < 2)
            {
                if (p != null) Tell(p, $"Használat: !{name[4..]} <név | @all | @ct | @t | @me>");
                return;
            }
            var targets = cmd.GetArgTargetResult(1).Players.Where(x => x.IsValid && !x.IsHLTV).ToList();
            if (targets.Count == 0)
            {
                if (p != null) Tell(p, "Nincs ilyen játékos.");
                return;
            }
            foreach (var t in targets) act(p, t);
            if (p != null)
                Tell(p, $"{{gold}}!{name[4..]}{{default}}: " + string.Join(", ", targets.Select(t => t.PlayerName)));
        });
    }

    private void MoveZ(CCSPlayerController t, float dz)
    {
        if (!t.PawnIsAlive) return;
        var pawn = t.PlayerPawn.Value!;
        var o = pawn.AbsOrigin;
        if (o == null) return;
        pawn.Teleport(new Vector(o.X, o.Y, o.Z + dz), null, null);
    }

    private void KillAfter(CounterStrikeSharp.API.Modules.Timers.Timer timer, float seconds, Action? after = null)
    {
        AddTimer(seconds, () => { timer.Kill(); after?.Invoke(); }, TimerFlags.STOP_ON_MAPCHANGE);
    }
}
