using System.Linq;
using RimWorld;
using Verse;

namespace YASTM
{
    public class RankVisualExtension : DefModExtension
    {
        public string pipApparelDefName;
    }

    public class CompProperties_PromoteToRank : CompProperties_UseEffect
    {
        public string rankTraitDef;   // z.B. "ST_Rank_Commander"
        public bool destroyOnUse = true;

        public CompProperties_PromoteToRank()
        {
            compClass = typeof(CompPromoteToRank);
        }
    }

    public class CompPromoteToRank : CompUseEffect
    {
        public CompProperties_PromoteToRank Props2 => (CompProperties_PromoteToRank)props;

        public override AcceptanceReport CanBeUsedBy(Pawn p)
        {
            var rep = base.CanBeUsedBy(p);
            if (!rep.Accepted) return rep;
            if (p == null || p.Dead) return false;

            var targetTrait = DefDatabase<TraitDef>.GetNamedSilentFail(Props2.rankTraitDef);
            if (targetTrait == null) return $"Missing TraitDef '{Props2.rankTraitDef}'.";

            if (!PromotionGating.MeetsRequirements(p, targetTrait, out var failReason))
                return failReason ?? "Not allowed.";

            return true;
        }

        public override void DoEffect(Pawn user)
        {
            base.DoEffect(user);
            if (user == null || user.Dead) return;

            var targetTrait = DefDatabase<TraitDef>.GetNamedSilentFail(Props2.rankTraitDef);
            if (targetTrait == null) return;

            // 1) vorhandene Rang-Traits entfernen
            TryClearExistingRankTraits(user);

            // 2) Rang setzen
            if (!user.story.traits.HasTrait(targetTrait))
                user.story.traits.GainTrait(new Trait(targetTrait));

            // 3) Pips automatisch anlegen
            TryAutoEquipPips(user, targetTrait);

            // 4) Zeitstempel für Mindestwartezeit
            WorldComponent_PromotionTracker.Get()?.RecordPromotion(user);

            // 5) Item verbrauchen
            if (Props2.destroyOnUse)
            {
                if (parent.stackCount > 1) parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                else parent.Destroy(DestroyMode.Vanish);
            }

            // 6) Feedback
            Messages.Message("ST.Promo.Success".Translate(user.Named("PAWN"), targetTrait.label.CapitalizeFirst()),
                user, MessageTypeDefOf.PositiveEvent);

            // 7) Kleiner „Ehre“-Gedächtnis-Gedanke (Mood-Boost für den Beförderten)
            var honorThought = DefDatabase<ThoughtDef>.GetNamedSilentFail("ST_PromotionHonored");
            if (honorThought != null)
                user.needs?.mood?.thoughts?.memories?.TryGainMemory(honorThought);

            // 8) Promotion Ceremony starten (vanilla Party als Feier)
            TryStartPromotionCeremony(user.Map, user);
        }

        private void TryClearExistingRankTraits(Pawn pawn)
        {
            var traits = pawn.story?.traits?.allTraits;
            if (traits == null || traits.Count == 0) return;

            for (int i = traits.Count - 1; i >= 0; i--)
            {
                var tr = traits[i]?.def;
                if (tr != null && tr.defName != null && tr.defName.StartsWith("ST_Rank_"))
                {
                    pawn.story.traits.RemoveTrait(traits[i]);
                }
            }
        }

        private void TryAutoEquipPips(Pawn pawn, TraitDef targetTrait)
        {
            if (pawn.apparel == null) return;

            // a) existierende Pips ablegen/entfernen
            var worn = pawn.apparel.WornApparel;
            for (int i = worn.Count - 1; i >= 0; i--)
            {
                var ap = worn[i];
                if (ap?.def?.defName != null && ap.def.defName.StartsWith("ST_RankPips_"))
                {
                    pawn.apparel.Remove(ap);     // Signatur: Remove(Apparel)
                    ap.Destroy(DestroyMode.Vanish);
                }
            }

            // b) neue Pips laut Extension anlegen
            var ext = targetTrait.GetModExtension<RankVisualExtension>();
            if (ext == null || string.IsNullOrEmpty(ext.pipApparelDefName)) return;

            var pipDef = DefDatabase<ThingDef>.GetNamedSilentFail(ext.pipApparelDefName);
            if (pipDef == null) return;

            var pip = ThingMaker.MakeThing(pipDef) as Apparel;
            if (pip == null) return;

            pawn.apparel.Wear(pip, true); // ersetzt Konflikte, dropt ggf. auf Boden
        }

        private void TryStartPromotionCeremony(Map map, Pawn honoree)
        {
            if (map == null || honoree == null || !honoree.Spawned) return;

            // Party-Def per Name holen (RW-Versionen unterscheiden sich beim IncidentDefOf)
            // Fallback auf Gathering_Friendly, falls "Party" nicht existiert.
            var partyDef = DefDatabase<IncidentDef>.GetNamedSilentFail("Party")
                        ?? DefDatabase<IncidentDef>.GetNamedSilentFail("Gathering_Friendly");

            if (partyDef != null)
            {
                var parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.Misc, map);
                parms.target = map;

                bool started = partyDef.Worker.TryExecute(parms);
                if (started)
                {
                    Messages.Message("ST.Promo.Ceremony.Started".Translate(honoree.Named("PAWN")),
                        new LookTargets(honoree), MessageTypeDefOf.PositiveEvent);
                }
            }
            // else: kein passender Incident in dieser Version – einfach stillschweigend kein Party-Start
        }
    }
}
