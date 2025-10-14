using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ST.PhaseWeapons
{
    // Comp + Props: an die Nahkampfwaffe hängen (z.B. Stun Baton)
    public class CompProperties_StunOnMelee : CompProperties
    {
        public int   stunTicks     = 240; // ~4s
        public bool  stunOnlyFlesh = true;
        public float chance        = 1f;  // 0..1
        public CompProperties_StunOnMelee() { compClass = typeof(CompStunOnMelee); }
    }

    public class CompStunOnMelee : ThingComp
    {
        public CompProperties_StunOnMelee Props => (CompProperties_StunOnMelee)props;
    }

    // Harmony: nach erfolgreichem Nahkampfangriff Betäubung anwenden
    [HarmonyPatch(typeof(Verb_MeleeAttack), "TryCastShot")]
    public static class Patch_Verb_MeleeAttack_TryCastShot
    {
        // Zugriff auf Verb.currentTarget (privates Feld)
        static readonly FieldInfo FI_CurrentTarget = AccessTools.Field(typeof(Verb), "currentTarget");

        public static void Postfix(Verb_MeleeAttack __instance, bool __result)
        {
            if (!__result) return; // Angriff hat nicht getroffen/ausgeführt
            var caster = __instance.CasterPawn;
            var eq     = __instance.EquipmentSource;
            if (caster == null || eq == null) return;

            var comp = eq.TryGetComp<CompStunOnMelee>();
            if (comp == null) return; // Waffe hat keinen Stun-Comp

            // Ziel ermitteln
            var lti    = (LocalTargetInfo)FI_CurrentTarget.GetValue(__instance);
            var target = lti.Thing as Pawn;
            if (target == null) return;

            var p = comp.Props;
            if (p.stunOnlyFlesh && !(target.RaceProps?.IsFlesh == true)) return;
            if (Rand.Value > p.chance) return;

            target.stances?.stunner?.StunFor(p.stunTicks, caster);

            // dezentes Feedback (optional, entferne wenn du’s nicht willst)
            MoteMaker.ThrowText(target.DrawPos, target.Map, "STUN", 1.2f);
        }
    }
}
