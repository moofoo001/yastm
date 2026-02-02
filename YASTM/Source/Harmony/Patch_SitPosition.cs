using HarmonyLib;
using UnityEngine;
using Verse;
using RimWorld;

namespace YASTM
{
    [HarmonyPatch(typeof(PawnRenderer), "GetBodyPos")] 
    public static class Patch_SitPosition
    {
        public static void Postfix(PawnRenderer __instance, ref Vector3 __result, Pawn ___pawn)
        {
            if (___pawn == null || !___pawn.Spawned) return;

            Building edifice = ___pawn.Position.GetEdifice(___pawn.Map);
            
            if (edifice != null)
            {
                var extension = edifice.def.GetModExtension<ST_SitOffsetExtension>();
                
                if (extension != null)
                {
                    Rot4 rotation = edifice.Rotation;

                    if (rotation == Rot4.North) 
                    {
                        __result += extension.offsetNorth;
                        // HIER IST DAS UPGRADE: Wir nutzen den Wert aus der XML
                        if (extension.northLayerAdjust != 0f)
                        {
                            __result.y += extension.northLayerAdjust;
                        }
                    }
                    else if (rotation == Rot4.South) __result += extension.offsetSouth;
                    else if (rotation == Rot4.East) __result += extension.offsetEast;
                    else if (rotation == Rot4.West) __result += extension.offsetWest;
                }
            }
        }
    }
}