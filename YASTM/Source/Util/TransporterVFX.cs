using RimWorld;
using Verse;
using Verse.Sound;
using UnityEngine;

namespace YASTM
{
    public static class TransporterVFX
    {
        private static FleckDef FleckStatic =>
            DefDatabase<FleckDef>.GetNamedSilentFail("ST_Fleck_TransporterColumn_Static");
        private static FleckDef FleckAttached =>
            DefDatabase<FleckDef>.GetNamedSilentFail("ST_Fleck_TransporterColumn_Attached");

      
        public static void PlayBeam(Map map, IntVec3 pos)
        {
            if (map == null) return;
            var center = pos.ToVector3Shifted();

            var snd = DefDatabase<SoundDef>.GetNamedSilentFail("ST_Transporter_Beam");
            if (snd != null)
                snd.PlayOneShot(SoundInfo.InMap(new TargetInfo(pos, map), MaintenanceType.None));

            FleckMaker.Static(center, map, FleckDefOf.PsycastAreaEffect, 1.35f);
            FleckMaker.Static(center, map, FleckDefOf.PsycastAreaEffect, 1.10f);
            FleckMaker.Static(center, map, FleckDefOf.PsycastAreaEffect, 0.90f);

            if (FleckStatic != null)
                FleckMaker.Static(center, map, FleckStatic, 1.45f);

            FleckMaker.Static(center, map, FleckDefOf.PsycastAreaEffect, 1.50f);
        }

     
        public static void BeginRematerialize(Pawn pawn, int durationTicks = 180)
        {
            if (pawn?.Map == null) return;
            pawn.Map.GetComponent<MapComponent_TransporterFX>()?
                .StartRematerialize(pawn, durationTicks);
        }

      
        public static void AttachBeamOverlayToPawn(Pawn pawn, float scale = 1.2f)
        {
            if (pawn == null || pawn.Map == null) return;
            var def = FleckAttached ?? FleckStatic;
            if (def == null) return;
            FleckMaker.AttachedOverlay(pawn, def, Vector3.zero, scale);
        }
    }
}

