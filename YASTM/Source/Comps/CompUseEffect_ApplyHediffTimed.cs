using RimWorld;
using Verse;

namespace YASTM
{
    public class CompProperties_UseHediffTimed : CompProperties_UseEffect
    {
        public string hediffDef;            // z.B. "ST_Hypo_Analgesic"
        public int minTicks = 12000;        // 0.2 Tage
        public int maxTicks = 24000;        // 0.4 Tage
        public float severity = 0f;         // optional
        public int minMedicalSkill = 0;     // optional
        public bool requireTalking = true;  // Sprechfähigkeit nötig?
        public bool destroyOnUse = true;    // <— NEU: ein Exemplar verbrauchen

        public CompProperties_UseHediffTimed()
        {
            this.compClass = typeof(CompUseEffect_ApplyHediffTimed);
        }
    }

    public class CompUseEffect_ApplyHediffTimed : CompUseEffect
    {
        public CompProperties_UseHediffTimed PropsTimed => (CompProperties_UseHediffTimed)props;

        public override AcceptanceReport CanBeUsedBy(Pawn p)
        {
            var rep = base.CanBeUsedBy(p);
            if (!rep.Accepted) return rep;

            if (PropsTimed.requireTalking && !p.health.capacities.CapableOf(PawnCapacityDefOf.Talking))
                return "ST.Hypo.NeedTalking".Translate(p.Named("PAWN"));

            if (PropsTimed.minMedicalSkill > 0)
            {
                int med = p.skills?.GetSkill(SkillDefOf.Medicine)?.Level ?? 0;
                if (med < PropsTimed.minMedicalSkill)
                    return "ST.Hypo.NeedMed".Translate(PropsTimed.minMedicalSkill);
            }
            return true;
        }

        public override void DoEffect(Pawn user)
        {
            base.DoEffect(user);
            if (user == null) return;

            
            var def = DefDatabase<HediffDef>.GetNamedSilentFail(PropsTimed.hediffDef);
            if (def == null)
            {
                Log.Error($"[YASTM] HediffDef '{PropsTimed.hediffDef}' not found for CompUseEffect_ApplyHediffTimed.");
                return;
            }

            var hediff = user.health.hediffSet.GetFirstHediffOfDef(def) ?? user.health.AddHediff(def);
            if (PropsTimed.severity > 0f) hediff.Severity = PropsTimed.severity;

            var disp = hediff.TryGetComp<HediffComp_Disappears>();
            if (disp != null)
            {
                int dur = Rand.RangeInclusive(PropsTimed.minTicks, PropsTimed.maxTicks);
                disp.ticksToDisappear = dur;
            }

            
            if (user.Map != null)
                MoteMaker.ThrowText(user.DrawPos, user.Map, "ST.Hypo.Applied".Translate(), 1.5f);

            
            if (PropsTimed.destroyOnUse)
            {
                if (parent.stackCount > 1)
                {
                    var one = parent.SplitOff(1);
                    one.Destroy(DestroyMode.Vanish);
                }
                else
                {
                    parent.Destroy(DestroyMode.Vanish);
                }
            }
        }
    }
}
