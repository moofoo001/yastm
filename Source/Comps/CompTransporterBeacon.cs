using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace YASTM
{
    public class CompProperties_TransporterBeacon : CompProperties
    {
        public int cooldownBeamInTicks = 300000;   // ~5 Tage
        public int cooldownBeamOutTicks = 120000;  // ~2 Tage
        public float maxBeamOutDistance = 50f;     // max. Ziel-Entfernung für Beam-Out
        public string soundDefName = "ST_Transporter_Beam";  // optionaler Sound

        public CompProperties_TransporterBeacon()
        {
            compClass = typeof(CompTransporterBeacon);
        }
    }

    public class CompTransporterBeacon : ThingComp
    {
        public CompProperties_TransporterBeacon Props => (CompProperties_TransporterBeacon)props;

        private int nextAllowedBeamInTick;
        private int nextAllowedBeamOutTick;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref nextAllowedBeamInTick, "ST_nextBeamIn", 0);
            Scribe_Values.Look(ref nextAllowedBeamOutTick, "ST_nextBeamOut", 0);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent.Faction != Faction.OfPlayer) yield break;
            var map = parent.Map;
            var power = parent.TryGetComp<CompPowerTrader>();

            // Beam-in Logistics (kleines Care-Paket)
            var beamIn = new Command_Action
            {
                defaultLabel = "ST.Transporter.BeamIn".Translate(),
                defaultDesc  = "ST.Transporter.BeamIn.Desc".Translate(),
                icon         = ContentFinder<Texture2D>.Get("UI/Commands/PodLaunch", false),
                action       = DoBeamIn
            };

            int now = Find.TickManager.TicksGame;
            if (power != null && !power.PowerOn)
                beamIn.Disable("ST.Common.RequiresPower".Translate());
            if (now < nextAllowedBeamInTick)
                beamIn.Disable("ST.Common.Recharging".Translate((nextAllowedBeamInTick - now).ToStringTicksToPeriod()));

            yield return beamIn;

            // Emergency Beam-out (Pawn auswählen)
            var beamOut = new Command_Target
            {
                defaultLabel = "ST.Transporter.BeamOut".Translate(),
                defaultDesc  = "ST.Transporter.BeamOut.Desc".Translate(),
                icon         = ContentFinder<Texture2D>.Get("UI/Commands/LoadTransporter", false),
                targetingParams = new TargetingParameters
                {
                    canTargetPawns = true,
                    canTargetLocations = false,
                    canTargetSelf = false,
                    validator = t =>
                    {
                        var p = t.Thing as Pawn;
                        if (p == null || p.Map != map) return false;
                        if (p.Faction != Faction.OfPlayer) return false;
                        // Optional: downed bevorzugt – aber nicht zwingend
                        return true;
                    }
                }
            };

            if (power != null && !power.PowerOn)
                beamOut.Disable("ST.Common.RequiresPower".Translate());
            if (now < nextAllowedBeamOutTick)
                beamOut.Disable("ST.Common.Recharging".Translate((nextAllowedBeamOutTick - now).ToStringTicksToPeriod()));

            beamOut.action = target => TryBeamOutPawn(target.Thing as Pawn);
            yield return beamOut;
        }

        // ---------- Beam-in ----------
        private void DoBeamIn()
        {
            var map = parent.Map;
            if (map == null) return;

            int now = Find.TickManager.TicksGame;
            if (now < nextAllowedBeamInTick) return;

            // Zielzelle: in der Nähe des Beacons
            if (!CellFinder.TryFindRandomCellNear(parent.Position, map, 3,
                    c => c.Standable(map) && c.InBounds(map) && !c.Fogged(map), out var cell))
                cell = parent.Position;

            // Kleines Care-Paket zusammenstellen
            var things = MakeLogisticsPackage();

            // Beamen: Items an Zelle platzieren, Effekte & Sound
            foreach (var t in things)
                GenPlace.TryPlaceThing(t, cell, map, ThingPlaceMode.Near);

            PlayTransporterFX(cell, map);

            Messages.Message("ST.Transporter.BeamIn.Done".Translate(), new LookTargets(cell, map), MessageTypeDefOf.PositiveEvent);

            nextAllowedBeamInTick = now + Props.cooldownBeamInTicks;
        }

        private List<Thing> MakeLogisticsPackage()
        {
            var list = new List<Thing>();
            void Add(string defName, int count)
            {
                var def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
                if (def == null || count <= 0) return;
                var thing = ThingMaker.MakeThing(def);
                thing.stackCount = Mathf.Min(count, def.stackLimit);
                // Falls mehr als stackLimit gewünscht: aufteilen
                int remaining = count - thing.stackCount;
                list.Add(thing);
                while (remaining > 0)
                {
                    var t = ThingMaker.MakeThing(def);
                    t.stackCount = Mathf.Min(remaining, def.stackLimit);
                    list.Add(t);
                    remaining -= t.stackCount;
                }
            }

            Add("MedicineIndustrial", 6);
            Add("PackageSurvivalMeal", 12);
            Add("ComponentIndustrial", 4);
            Add("Steel", 80);

            return list;
        }

        // ---------- Beam-out ----------
        private void TryBeamOutPawn(Pawn pawn)
        {
            var map = parent.Map;
            if (pawn == null || map == null) return;

            int now = Find.TickManager.TicksGame;
            if (now < nextAllowedBeamOutTick) return;

            // Reichweitencheck
            if (pawn.Position.DistanceTo(parent.Position) > Props.maxBeamOutDistance)
            {
                Messages.Message("ST.Transporter.BeamOut.TooFar".Translate(), pawn, MessageTypeDefOf.RejectInput);
                return;
            }

            // Ziel: beim Beacon oder angrenzend
            IntVec3 dest = parent.Position;
            if (!dest.Walkable(map))
                dest = CellFinder.StandableCellNear(parent.Position, map, 1);

            // Effekte am Start
            PlayTransporterFX(pawn.Position, map);

            // Teleport
            if (pawn.Spawned) pawn.DeSpawn();
            GenSpawn.Spawn(pawn, dest, map);

            // Effekte am Ziel
            PlayTransporterFX(dest, map);

            // Leichte Übelkeit (kurzer Debuff)
            var nausea = DefDatabase<HediffDef>.GetNamedSilentFail("ST_TransporterNausea");
            if (nausea != null)
            {
                var h = pawn.health.AddHediff(nausea);
                var disp = h.TryGetComp<HediffComp_Disappears>();
                if (disp != null) disp.ticksToDisappear = Rand.RangeInclusive(15000, 25000); // ~0.25–0.4 Tage
            }

            Messages.Message("ST.Transporter.BeamOut.Done".Translate(pawn.Named("PAWN")), pawn, MessageTypeDefOf.PositiveEvent);

            nextAllowedBeamOutTick = now + Props.cooldownBeamOutTicks;
        }

        private void PlayTransporterFX(IntVec3 cell, Map map)
        {
            // Sound
            if (!Props.soundDefName.NullOrEmpty())
            {
                var sd = DefDatabase<SoundDef>.GetNamedSilentFail(Props.soundDefName);
                if (sd != null) SoundStarter.PlayOneShot(sd, new TargetInfo(cell, map));
            }
            // Flecks
            FleckMaker.Static(cell.ToVector3Shifted(), map, FleckDefOf.MicroSparks, 1.2f);
            FleckMaker.Static(cell.ToVector3Shifted(), map, FleckDefOf.AirPuff, 1.0f);
        }
    }
}
