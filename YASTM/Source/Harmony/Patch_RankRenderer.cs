using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld; // WICHTIG für 'Trait' und 'PawnRenderNode'
using HarmonyLib;

namespace YASTM
{
    [HarmonyPatch(typeof(PawnRenderNode_Body), "GraphicFor")]
    public static class Patch_RankRenderer
    {
        // WICHTIG: __result MUSS 'ref Graphic' sein, NICHT 'ref string'!
        public static void Postfix(PawnRenderNode_Body __instance, Pawn pawn, ref Graphic __result)
        {
            if (pawn == null || pawn.story == null || pawn.story.traits == null) return;

            foreach (Trait trait in pawn.story.traits.allTraits)
            {
                var extension = trait.def.GetModExtension<RankVisualExtension>();
                if (extension == null) continue;

                string texFileName = null;

                // FALL A: Grad-System
                if (extension.ranks != null)
                {
                    var data = extension.ranks.Find(r => r.degree == trait.Degree);
                    if (data != null) texFileName = data.texName;
                }
                // FALL B: Einzel-Name
                else if (!string.IsNullOrEmpty(extension.texName))
                {
                    texFileName = extension.texName;
                }

                if (texFileName != null)
                {
                    string fullPath = extension.rankTexPath + texFileName;
                    // Log.Message($"[YASTM] Rank Visual Path: {fullPath}"); 
                    // Hier später die Render-Logik (Overlay) einfügen
                }
            }
        }
    }
}