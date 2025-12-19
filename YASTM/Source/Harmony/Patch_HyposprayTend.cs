using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;
using YASTM.Source.Comps;
using System;
using System.Reflection;
using System.Linq;

namespace YASTM.Source.HarmonyPatches
{

    [HarmonyPatch(typeof(TendUtility), "DoTend")]
    public static class Patch_HyposprayTend
    {
        public static void Postfix(Pawn doctor, Pawn patient, Medicine medicine)
        {
            if (doctor == null) return;
            CompHypospray hypo = GetHypospray(doctor);

            if (hypo != null && hypo.CanTend())
            {
                hypo.UseCharge();
            }
        }

        public static CompHypospray GetHypospray(Pawn pawn)
        {
            if (pawn.equipment?.Primary != null)
            {
                var c = pawn.equipment.Primary.TryGetComp<CompHypospray>();
                if (c != null) return c;
            }
            if (pawn.apparel?.WornApparel != null)
            {
                foreach (var ap in pawn.apparel.WornApparel)
                {
                    var c = ap.TryGetComp<CompHypospray>();
                    if (c != null) return c;
                }
            }
            return null;
        }
    }


    [HarmonyPatch] 
    public static class Patch_HyposprayQuality
    {
        static MethodBase TargetMethod()
        {

            var method = AccessTools.GetDeclaredMethods(typeof(TendUtility))
                .Where(m => m.Name == "CalculateBaseTendQuality")
                .OrderByDescending(m => m.GetParameters().Length)
                .FirstOrDefault();

            if (method == null)
            {
                Log.Error("[YASTM] Critical: Could not find CalculateBaseTendQuality to patch!");
            }
            return method;
        }

        public static void Postfix(Pawn doctor, ref float __result)
        {
            if (doctor == null) return;
            

            CompHypospray hypo = Patch_HyposprayTend.GetHypospray(doctor);

            if (hypo != null && hypo.CanTend())
            {
                float bonus = hypo.Props.tendQualityOffset;
                __result += bonus;

                __result = Mathf.Clamp(__result, 0f, 1.3f);
            }
        }
    }
}