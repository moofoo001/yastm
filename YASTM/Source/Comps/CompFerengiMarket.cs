using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace YASTM
{
    public class CompProperties_FerengiMarket : CompProperties
    {
        public TraderKindDef marketTraderKind;
        public CompProperties_FerengiMarket()
        {
            this.compClass = typeof(CompFerengiMarket);
        }
    }

    public class CompFerengiMarket : ThingComp
    {
        public CompProperties_FerengiMarket Props => (CompProperties_FerengiMarket)props;
        
        private TradeShip virtualTrader;
        private int lastRestockTick = -99999;

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent.GetComp<CompPowerTrader>()?.PowerOn == true)
            {
                yield return new Command_Action
                {
                    defaultLabel = "ST_OpenFerengiMarket".Translate(),
                    defaultDesc = "ST_OpenFerengiMarketDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("Things/Item/Resource/ST_Latinum/ST_Latinum_b", true),
                    action = () => 
                    {
                        List<FloatMenuOption> options = new List<FloatMenuOption>();
                        foreach (Pawn p in parent.Map.mapPawns.FreeColonistsSpawned)
                        {
                            if (p.health.State == PawnHealthState.Mobile)
                            {
                                string label = p.LabelShort + " (" + p.skills.GetSkill(SkillDefOf.Social).Level + ")";
                                options.Add(new FloatMenuOption(label, () => OpenMarket(p)));
                            }
                        }
                        Find.WindowStack.Add(new FloatMenu(options));
                    }
                };
            }
        }

        private void OpenMarket(Pawn negotiator)
        {
            // Sicherstellen, dass Map existiert
            if (parent.Map == null) return;

            // Restock Logik
            if (virtualTrader == null || Find.TickManager.TicksGame - lastRestockTick > 180000)
            {
                GenerateVirtualTrader();
            }
            
            // SICHERHEITS-CHECK: Hat das Schiff den Map-Link verloren? (Kann beim Laden passieren)
            if (virtualTrader != null && virtualTrader.passingShipManager == null)
            {
                virtualTrader.passingShipManager = parent.Map.passingShipManager;
            }

            if (virtualTrader != null)
            {
                TradeSession.SetupWith(virtualTrader, negotiator, false);
                Find.WindowStack.Add(new Dialog_Trade(negotiator, virtualTrader, false));
                
                if (Rand.Chance(0.5f))
                {
                    Messages.Message("ST_FerengiRuleQuote".Translate(), MessageTypeDefOf.NeutralEvent);
                }
            }
        }

        private void GenerateVirtualTrader()
        {
            // 1. Suche die Ferengi Fraktion (oder Fallback auf Spacer/Ancients, damit es nicht crasht)
            Faction tradeFaction = Find.FactionManager.FirstFactionOfDef(DefDatabase<FactionDef>.GetNamed("ST_Faction_Ferengi", false));
            
            // Fallback: Wenn Ferengi nicht gefunden (z.B. nicht gespawnt), nimm eine neutrale Fraktion
            if (tradeFaction == null)
            {
                tradeFaction = Find.FactionManager.AllFactions.FirstOrDefault(f => !f.IsPlayer && !f.HostileTo(Faction.OfPlayer));
            }

            // Wenn immer noch null (sehr unwahrscheinlich), Abbruch
            if (tradeFaction == null) 
            {
                Log.Error("[YASTM] Could not find any faction for Ferengi Market.");
                return;
            }

            // 2. Schiff erstellen mit KORREKTER Fraktion
            virtualTrader = new TradeShip(Props.marketTraderKind, tradeFaction);

            // 3. WICHTIGSTER FIX: Dem Schiff den Map-Manager zuweisen!
            // Ohne das crasht es, weil es keine Orbital Beacons findet.
            virtualTrader.passingShipManager = parent.Map.passingShipManager;

            // 4. Waren generieren
            virtualTrader.GenerateThings();
            lastRestockTick = Find.TickManager.TicksGame;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref lastRestockTick, "lastRestockTick", -99999);

        }
    }
}