// File: Source/Systems/SelectClickSound.cs

using System;
using HarmonyLib;
using Verse;
using Verse.Sound;

namespace YASTM.UI
{
    [StaticConstructorOnStartup]
    public static class SelectClickSound_Init
    {
        static SelectClickSound_Init()
        {
            try
            {
                var h = new Harmony("YASTM.SelectClickSound");

           
                var tSelector =
                    AccessTools.TypeByName("RimWorld.Selector") ??
                    AccessTools.TypeByName("Verse.Selector");

                if (tSelector == null) return;

           
                var mSelect = AccessTools.Method(tSelector, "Select", new Type[]
                {
                    typeof(object), typeof(bool), typeof(bool)
                });
                if (mSelect == null) return;

                h.Patch(mSelect,
                    postfix: new HarmonyMethod(typeof(SelectClickSound_Init), nameof(Postfix_Select)));
            }
            catch (Exception e)
            {
                Log.Warning($"[YASTM][SelectClickSound] Init failed: {e}");
            }
        }

   
        public static void Postfix_Select(object obj, bool playSound)
        {
            try
            {
                if (!playSound || obj == null) return;

                if (obj is Thing thing
                    && thing.Map != null
                    && thing.def?.defName != null
                    && thing.def.defName.StartsWith("ST_"))
                {
                    var snd = DefDatabase<SoundDef>.GetNamedSilentFail("ST_SFX_LCARS_Click");
                    if (snd != null)
                        snd.PlayOneShot(new TargetInfo(thing.Position, thing.Map));
                }
            }
            catch (Exception e)
            {
           
                Log.Warning($"[YASTM][SelectClickSound] Postfix error: {e}");
            }
        }
    }
}

