using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ST.PhaseWeapons
{
    [StaticConstructorOnStartup]
    public static class Startup
    {
        static Startup()
        {
            new Harmony("ST.PhaseWeapons").PatchAll();
            Log.Message("[PhaserMode] Harmony patched.");
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class Pawn_GetGizmos_Patch
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn __instance)
        {
            foreach (var g in __result) yield return g;

            var eq = __instance?.equipment;
            if (eq == null) yield break;

            foreach (var gear in eq.AllEquipmentListForReading)
            {
                var comp = PhaserUtil.GetPhaserComp(gear as ThingWithComps);
                if (comp == null) continue;


                var stunCmd = new Command_Action();
                stunCmd.hotKey = null; 
                void SetStunVisuals()
                {
                    bool on = comp.mode == PhaserFireMode.Stun;
                    stunCmd.defaultLabel = on ? "Stun: ON" : "Stun: OFF";
                    stunCmd.defaultDesc  = on ? "Disable stun mode." : "Enable stun mode.";
                    stunCmd.icon = ContentFinder<Texture2D>.Get(
                        on ? "Things/Projectile/PhaserPulse_Stun" : "Things/Projectile/PhaserPulse", false);
                }
                SetStunVisuals();
                stunCmd.action = () =>
                {
                    Log.Message($"[Phaser2Btn][CLICK-STUN] {gear.def.defName} {gear.ThingID} pre={comp.mode}");
                    comp.ToggleStun();
                    if (comp.mode == PhaserFireMode.Stun) { /* nichts weiter nötig */ }
                    SetStunVisuals();
                    SoundDefOf.Click.PlayOneShot(SoundInfo.OnCamera());
                    Log.Message($"[Phaser2Btn][CLICK-STUN] post={comp.mode}");
                };
                yield return stunCmd;

                if (comp.Props?.allowOvercharge ?? false)
                {
                    var ocCmd = new Command_Action();
                    ocCmd.hotKey = null;
                    void SetOcVisuals()
                    {
                        bool on = comp.mode == PhaserFireMode.Overcharge;
                        ocCmd.defaultLabel = on ? "Overcharge: ON" : "Overcharge: OFF";
                        ocCmd.defaultDesc  = on ? "Disable overcharge." : "Enable overcharge.";
                        ocCmd.icon = ContentFinder<Texture2D>.Get(
                            on ? "Things/Projectile/PhaserPulse_Overcharge" : "Things/Projectile/PhaserPulse", false);
                    }
                    SetOcVisuals();
                    ocCmd.action = () =>
                    {
                        //Log.Message($"[Phaser2Btn][CLICK-OC] {gear.def.defName} {gear.ThingID} pre={comp.mode}");
                        comp.ToggleOvercharge();

                        if (comp.mode == PhaserFireMode.Overcharge) { /* nichts weiter nötig */ }
                        SetOcVisuals();
                        SoundDefOf.Click.PlayOneShot(SoundInfo.OnCamera());
                        //Log.Message($"[Phaser2Btn][CLICK-OC] post={comp.mode}");
                    };
                    yield return ocCmd;
                }
            }
        }
    }
}