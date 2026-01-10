using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace YASTM
{
    public class CompProperties_DiplomacyComms : CompProperties
    {
        public int improveCost = 300;
        public int improveGoodwill = 10;
        public int ceasefireCost = 600;
        public float cooldownDaysImprove = 1f;
        public float cooldownDaysCeasefire = 2f;

        public int minSocialImprove = 6;
        public int minSocialCeasefire = 10;

        public List<string> requiredTraitsImprove;
        public List<string> requiredApparelsImprove;
        public List<string> requiredTraitsCeasefire;
        public List<string> requiredApparelsCeasefire;

        public string rankLabelImprove = "commissioned officer";
        public string rankLabelCeasefire = "senior officer";

        public CompProperties_DiplomacyComms()
        {
            compClass = typeof(CompDiplomacyComms);
        }
    }

    public class CompDiplomacyComms : ThingComp
    {
        public CompProperties_DiplomacyComms Props => (CompProperties_DiplomacyComms)props;
        MapComponent_Diplomacy MC => parent.Map?.GetComponent<MapComponent_Diplomacy>();

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent.Faction != Faction.OfPlayer) yield break;

            var power = parent.GetComp<CompPowerTrader>();
            bool powered = power == null || power.PowerOn;
            int now = Find.TickManager.TicksGame;
            var map = parent.Map;

            // Improve relations
            var gImprove = new Command_Action
            {
                defaultLabel = "ST.Diplo.Improve.Button".Translate(),
                defaultDesc  = "ST.Diplo.Improve.Desc".Translate(Props.improveCost, Props.improveGoodwill),
                icon         = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/Diplomacy", false),
                action       = OpenImproveMenu
            };
            
            // Checks ...
            if (!powered) gImprove.Disable("ST.Common.NeedsPower".Translate());
            else if (MC != null && now < MC.NextImproveTick) gImprove.Disable("ST.Common.Recharging".Translate((MC.NextImproveTick - now).ToStringTicksToPeriod()));
            else if (map != null && !HasEligibleOfficer(map, Props.requiredTraitsImprove, Props.requiredApparelsImprove, Props.minSocialImprove, out _))
                gImprove.Disable("ST.Diplo.Requirements.Improve".Translate(Props.minSocialImprove, Props.rankLabelImprove));
            
            yield return gImprove;

            // Request ceasefire
            var gCease = new Command_Action
            {
                defaultLabel = "ST.Diplo.Ceasefire.Button".Translate(),
                defaultDesc  = "ST.Diplo.Ceasefire.Desc".Translate(Props.ceasefireCost),
                icon         = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/CeaseFire", false),
                action       = OpenCeasefireMenu
            };

            // Checks ...
            if (!powered) gCease.Disable("ST.Common.NeedsPower".Translate());
            else if (MC != null && now < MC.NextCeaseTick) gCease.Disable("ST.Common.Recharging".Translate((MC.NextCeaseTick - now).ToStringTicksToPeriod()));
            else if (map != null && !HasEligibleOfficer(map, Props.requiredTraitsCeasefire, Props.requiredApparelsCeasefire, Props.minSocialCeasefire, out _))
                gCease.Disable("ST.Diplo.Requirements.Ceasefire".Translate(Props.minSocialCeasefire, Props.rankLabelCeasefire));
            
            yield return gCease;
        }

        // ---------- Improve relations ----------

        void OpenImproveMenu()
        {
            var map = parent.Map; if (map == null) return;
            var list = EligibleForImprove().Select(f => new FloatMenuOption(f.GetCallLabel(), () => ConfirmImprove(f))).ToList();
            if (list.Count == 0) { Messages.Message("ST.Diplo.Improve.None".Translate(), MessageTypeDefOf.RejectInput); return; }
            Find.WindowStack.Add(new FloatMenu(list));
        }

        IEnumerable<Faction> EligibleForImprove()
        {
            return Find.FactionManager.AllFactionsVisible.Where(f =>
                f != Faction.OfPlayer &&
                !f.HostileTo(Faction.OfPlayer) &&
                !f.def.hidden &&
                (f.def.humanlikeFaction || f.def.permanentEnemy == false) &&
                !f.defeated &&
                f.GoodwillWith(Faction.OfPlayer) < 100
            );
        }

        void ConfirmImprove(Faction f)
        {
            int cost = Props.improveCost, gain = Props.improveGoodwill;
            string txt = "ST.Diplo.Improve.Confirm".Translate(f.NameColored, cost, gain);
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(txt, () => DoImprove(f, cost, gain)));
        }

        void DoImprove(Faction f, int cost, int gain)
        {
            var map = parent.Map; if (map == null) return;
            
            // HIER IST DIE ÄNDERUNG: Wir holen uns den 'officer' aus der out-Variable
            if (!HasEligibleOfficer(map, Props.requiredTraitsImprove, Props.requiredApparelsImprove, Props.minSocialImprove, out Pawn officer))
            { 
                Messages.Message("ST.Diplo.Requirements.Improve".Translate(Props.minSocialImprove, Props.rankLabelImprove), MessageTypeDefOf.RejectInput); 
                return; 
            }

            if (!TradeUtility.ColonyHasEnoughSilver(map, cost))
            { 
                Messages.Message("ST.Common.NotEnoughSilver".Translate(cost), parent, MessageTypeDefOf.RejectInput); 
                return; 
            }

            TradeUtility.LaunchSilver(map, cost);

            int current = f.GoodwillWith(Faction.OfPlayer); 
            int delta = Mathf.Min(gain, Mathf.Max(0, 100 - current));
            if (delta <= 0) { Messages.Message("ST.Diplo.Improve.Maxed".Translate(f.NameColored), MessageTypeDefOf.RejectInput); return; }

            Faction.OfPlayer.TryAffectGoodwillWith(f, delta, true, true, null, parent);
            Messages.Message("ST.Diplo.Improve.Success".Translate(f.NameColored, delta), parent, MessageTypeDefOf.PositiveEvent);

            // KARRIERE PUNKTE VERGEBEN
            var career = officer.TryGetComp<CompCareer>();
            if (career != null)
            {
                career.AddCareerPoint("DiplomacyImprove", 1);
                // Optional: Feedback, dass der Offizier das gut gemacht hat
                MoteMaker.ThrowText(officer.DrawPos, officer.Map, "+Diplomacy", 3f);
            }

            int now = Find.TickManager.TicksGame;
            int cd = (int)(Props.cooldownDaysImprove * 60000f);
            if (MC != null) MC.NextImproveTick = now + cd;
            Find.SignalManager.SendSignal(new Signal("STQ_DiplomacyImproveCompleted"));
        }

        // ---------- Ceasefire ----------

        void OpenCeasefireMenu()
        {
            var map = parent.Map; if (map == null) return;
            var list = EligibleForCeasefire().Select(f => new FloatMenuOption(f.GetCallLabel(), () => ConfirmCeasefire(f))).ToList();
            if (list.Count == 0) { Messages.Message("ST.Diplo.Ceasefire.None".Translate(), MessageTypeDefOf.RejectInput); return; }
            Find.WindowStack.Add(new FloatMenu(list));
        }

        IEnumerable<Faction> EligibleForCeasefire()
        {
            return Find.FactionManager.AllFactionsVisible.Where(f =>
                f != Faction.OfPlayer &&
                f.HostileTo(Faction.OfPlayer) &&
                !f.def.hidden &&
                (f.def.humanlikeFaction || f.def.permanentEnemy == false) &&
                !f.defeated
            );
        }

        void ConfirmCeasefire(Faction f)
        {
            int cost = Props.ceasefireCost;
            string txt = "ST.Diplo.Ceasefire.Confirm".Translate(f.NameColored, cost);
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(txt, () => DoCeasefire(f, cost)));
        }

        void DoCeasefire(Faction f, int cost)
        {
            var map = parent.Map; if (map == null) return;
            
            // HIER IST DIE ÄNDERUNG: Wir holen uns den 'officer'
            if (!HasEligibleOfficer(map, Props.requiredTraitsCeasefire, Props.requiredApparelsCeasefire, Props.minSocialCeasefire, out Pawn officer))
            { 
                Messages.Message("ST.Diplo.Requirements.Ceasefire".Translate(Props.minSocialCeasefire, Props.rankLabelCeasefire), MessageTypeDefOf.RejectInput); 
                return; 
            }

            if (!TradeUtility.ColonyHasEnoughSilver(map, cost))
            { 
                Messages.Message("ST.Common.NotEnoughSilver".Translate(cost), parent, MessageTypeDefOf.RejectInput); 
                return; 
            }

            TradeUtility.LaunchSilver(map, cost);

            int current = f.GoodwillWith(Faction.OfPlayer); 
            int delta = Mathf.Max(0, 0 - current);
            if (delta > 0)
                Faction.OfPlayer.TryAffectGoodwillWith(f, delta, true, true, null, parent);

            if (f.HostileTo(Faction.OfPlayer))
                f.TryAffectGoodwillWith(Faction.OfPlayer, 1, false, false); 

            Messages.Message("ST.Diplo.Ceasefire.Success".Translate(f.NameColored), parent, MessageTypeDefOf.PositiveEvent);

            // KARRIERE PUNKTE VERGEBEN
            var career = officer.TryGetComp<CompCareer>();
            if (career != null)
            {
                // Friedensverträge sind "große" diplomatische Erfolge
                career.AddCareerPoint("DiplomacyCeasefire", 1);
                MoteMaker.ThrowText(officer.DrawPos, officer.Map, "+Peacemaker", 3f);
            }

            int now = Find.TickManager.TicksGame;
            int cd = (int)(Props.cooldownDaysCeasefire * 60000f);
            if (MC != null) MC.NextCeaseTick = now + cd;
            Find.SignalManager.SendSignal(new Signal("STQ_DiplomacyImproveCompleted")); // Evtl neuen Signalnamen nutzen? "STQ_CeasefireCompleted"
        }

        // ---------- Helpers ----------

        bool HasEligibleOfficer(Map map, List<string> reqTraits, List<string> reqApps, int minSocial, out Pawn chosen)
        {
            chosen = null;
            int best = -1;

            foreach (var p in map.mapPawns.FreeColonistsSpawned)
            {
                if (p.Downed || p.InMentalState) continue;
                if (!p.health.capacities.CapableOf(PawnCapacityDefOf.Talking)) continue;

                int social = p.skills?.GetSkill(SkillDefOf.Social)?.Level ?? 0;
                if (social < minSocial) continue;

                bool rankOK = RankTraitOk(p, reqTraits) || RankApparelOk(p, reqApps);
                if (!rankOK) continue;

                if (social > best) { best = social; chosen = p; }
            }
            return chosen != null;
        }

        bool RankTraitOk(Pawn p, List<string> required)
        {
            if (required == null || required.Count == 0) return true;
            var traits = p?.story?.traits?.allTraits;
            if (traits == null) return false;

            if (required.Contains("*ANY_COMMISSIONED*"))
                return traits.Any(t => t?.def?.defName != null && t.def.defName.StartsWith("ST_Rank_"));

            return traits.Any(t => t?.def != null && required.Contains(t.def.defName));
        }

        bool RankApparelOk(Pawn p, List<string> required)
        {
            if (required == null || required.Count == 0) return true;
            var wa = p?.apparel?.WornApparel;
            if (wa == null) return false;

            if (required.Contains("*ANY_PIP*"))
                return wa.Any(a => a?.def?.defName != null && a.def.defName.StartsWith("ST_RankPips_"));

            return wa.Any(a => a?.def != null && required.Contains(a.def.defName));
        }
    }

    public static class FactionCallLabelExt
    {
        public static string GetCallLabel(this Faction f)
        {
            string status = f.HostileTo(Faction.OfPlayer) ? "hostile".TranslateSimple() : "neutral".TranslateSimple();
            return $"{f.Name} ({status})";
        }
    }
}