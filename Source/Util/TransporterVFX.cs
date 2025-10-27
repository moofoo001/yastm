// Source/Util/TransporterVFX.cs
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;

namespace YASTM
{
    public static class TransporterVFX
    {
        private static FleckDef GetBlinkFleck()
        {
            // 1) Mod-Def, 2) vanilla MicroSparks, 3) AirPuff (Fallback)
            return DefDatabase<FleckDef>.GetNamedSilentFail("ST_TransporterBlink")
                ?? DefDatabase<FleckDef>.GetNamedSilentFail("MicroSparks")
                ?? DefDatabase<FleckDef>.GetNamedSilentFail("AirPuff");
        }

        public static void PlayBeam(Map map, IntVec3 cell)
        {
            if (map == null || !cell.InBounds(map)) return;

            // Fleck
            var fleck = GetBlinkFleck();
            if (fleck != null)
                FleckMaker.Static(cell, map, fleck, 1.2f);

            // Sound (optional)
            var snd = DefDatabase<SoundDef>.GetNamedSilentFail("ST_Transporter_Beam");
            if (snd != null)
            {
                var info = SoundInfo.InMap(new TargetInfo(cell, map), MaintenanceType.None);
                SoundStarter.PlayOneShot(snd, info);
            }
        }
    }
}
