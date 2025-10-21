using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace YASTM
{
    public class WorkGiver_UseMedicalTricorder : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            var list = pawn.Map.mapPawns.FreeColonistsAndPrisonersSpawned;
            for (int i = 0; i < list.Count; i++)
            {
                var p = list[i];
                if (p == pawn || p.Dead) continue;
                if (HealthAIUtility.ShouldBeTendedNowByPlayer(p)) yield return p;
            }
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var patient = t as Pawn;
            if (patient == null || patient.Dead) return false;
            if (patient.Faction != pawn.Faction) return false;
            if (!pawn.CanReserve(patient)) return false;

            var tri = GetTricorder(pawn);
            if (tri == null) return false;

            // Skill-Gate & Reichweite aus den Props
            int med = pawn.skills?.GetSkill(SkillDefOf.Medicine)?.Level ?? 0;
            if (med < tri.Props.minMedicine) return false;
            if (pawn.Position.DistanceTo(patient.Position) > tri.Props.range) return false;

            // Gemeinsamer Cooldown respektieren (falls vorhanden)
            int now = Find.TickManager.TicksGame;
            var shared = tri.parent.TryGetComp<CompTricorderSharedCooldown>();
            if (shared != null && !shared.IsReady(now)) return false;

            // Nicht doppeln: Wenn der Patient den Hediff schon hat, kein Scan
            var hd = DefDatabase<HediffDef>.GetNamedSilentFail(tri.HediffDefName);
            if (hd != null && patient.health.hediffSet.HasHediff(hd)) return false;

            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return new Job(DefDatabase<JobDef>.GetNamed("ST_ScanAllyMedical"), t);
        }

        private CompTricorderMedical GetTricorder(Pawn p)
        {
            var worn = p.apparel?.WornApparel;
            if (worn == null) return null;
            for (int i = 0; i < worn.Count; i++)
            {
                var c = worn[i].TryGetComp<CompTricorderMedical>();
                if (c != null) return c;
            }
            return null;
        }
    }
}
