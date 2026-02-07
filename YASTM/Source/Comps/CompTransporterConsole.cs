using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace YASTM
{
    public class CompProperties_TransporterConsole : CompProperties
    {
        public float energyCostPerUse = 1200f;
        public float tacticalBeamEnergyCost = 2500f;
        public int maxLinkedPads = 5;
        public int warmupTicks = 60;
        public int cooldownTicks = 1200;
        public float maxLinkDistance = 30f;
        
        public CompProperties_TransporterConsole()
        {
            this.compClass = typeof(CompTransporterConsole);
        }
    }

    public class CompTransporterConsole : ThingComp
    {
        public CompProperties_TransporterConsole Props => (CompProperties_TransporterConsole)this.props;

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo g in base.CompGetGizmosExtra())
            {
                yield return g;
            }

            if (this.parent.Faction == Faction.OfPlayer)
            {
                List<Building> pads = TransporterPadUtil.GetLinkedPads(this.parent as Building, Props.maxLinkedPads);
                bool systemsOnline = TransporterPadUtil.IsPoweredOn(this.parent) && pads.Count > 0;
                
                // ---Transporter Chief ---
                bool engineerPresent = IsEngineerManning();
                string engineerStatus = engineerPresent ? "Engineer present" : "Missing Transporter Chief";
                // --------------------------------------

                string status = systemsOnline ? $"Online ({pads.Count} Pads active)" : "Offline / No Pads";

                // 1. BEAM OUT
                Command_Action beamOut = new Command_Action();
                beamOut.defaultLabel = "Tactical Beam Out (Team)";
                beamOut.defaultDesc = $"Beam targets from pads.\n\nSystem: {status}\nOperator: {engineerStatus}";
                beamOut.icon = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/ST_BeamOut");
                
                if (!systemsOnline) 
                    beamOut.Disable("No Power or No Pads linked");
                else if (!engineerPresent)
                    beamOut.Disable("Requires 'Transporter Chief' trait OR Intellectual skill 10+."); // Sperre

                beamOut.action = delegate { StartTacticalTargeting_Out(pads); };
                yield return beamOut;

                // 2. BEAM IN
                Command_Action beamIn = new Command_Action();
                beamIn.defaultLabel = "Tactical Beam In (Team)";
                beamIn.defaultDesc = $"Beam targets to pads.\n\nSystem: {status}\nOperator: {engineerStatus}";
                beamIn.icon = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/ST_BeamIn");
                
                if (!systemsOnline) 
                    beamIn.Disable("No Power or No Pads linked");
                else if (!engineerPresent)
                    beamIn.Disable("Requires 'Transporter Chief' trait OR Intellectual skill 10+."); // Sperre
                
                beamIn.action = delegate { StartTacticalTargeting_In(pads); };
                yield return beamIn;
            }
        }

        private bool IsEngineerManning()
        {
            if (this.parent.Map == null) return false;

            // Hole die Dinge auf dem Stuhl
            IntVec3 interactionCell = this.parent.InteractionCell;
            List<Thing> thingsOnCell = interactionCell.GetThingList(this.parent.Map);

            foreach (Thing t in thingsOnCell)
            {
                if (t is Pawn p && p.Faction == Faction.OfPlayer)
                {
                    // CHECK 1: Der "Idiotensichere" Trait-Check
                    // Wir iterieren durch die Traits und prüfen den ANGEZEIGTEN NAMEN (Label).
                    // Das umgeht alle Probleme mit defNames (ST_TransporterChief vs ST_TransporterEngineer).
                    if (p.story?.traits?.allTraits != null)
                    {
                        foreach (Trait trait in p.story.traits.allTraits)
                        {
                            // Prüft, ob der Trait "Transporter Chief" heißt (egal wie die ID ist)
                            if (trait.Label.IndexOf("Transporter Chief", System.StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                return true;
                            }
                        }
                    }

                    // CHECK 2: Fallback für Genies (Intellectual 10+)
                    // Damit funktioniert Semna immer noch.
                    if (p.skills != null && p.skills.GetSkill(SkillDefOf.Intellectual).Level >= 10)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        // --- Tactical Targeting Logic ---
        private void StartTacticalTargeting_Out(List<Building> pads)
        {
            Find.Targeter.BeginTargeting(new TargetingParameters
            {
                canTargetLocations = true,
                validator = (TargetInfo x) => x.Cell.Walkable(this.parent.Map) && !x.Cell.Fogged(this.parent.Map)
            }, (LocalTargetInfo target) =>
            {
                ExecuteMultiBeam_Out(pads, target.Cell);
            });
        }

        private void ExecuteMultiBeam_Out(List<Building> pads, IntVec3 targetCenter)
        {
            List<Pawn> pawnsToBeam = new List<Pawn>();
            foreach(var pad in pads)
            {
                IntVec3 cell = TransporterPadUtil.GetPadCell(pad);
                var pawnsHere = cell.GetThingList(pad.Map).OfType<Pawn>().ToList();
                pawnsToBeam.AddRange(pawnsHere);
            }

            if (pawnsToBeam.NullOrEmpty())
            {
                Messages.Message("No life signs on any Transporter Pad.", this.parent, MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (!TryDrainPower(Props.tacticalBeamEnergyCost)) return;

            foreach (Pawn p in pawnsToBeam)
            {
                TransporterVFX.PlayBeam(p.Map, p.Position); 
                p.DeSpawn(DestroyMode.Vanish);
                IntVec3 validCell = CellFinder.RandomClosewalkCellNear(targetCenter, this.parent.Map, 2, null);
                GenSpawn.Spawn(p, validCell, this.parent.Map);
                TransporterVFX.PlayBeam(this.parent.Map, validCell); 
                ApplyTransporterSickness(p);
            }
            Messages.Message($"Tactical transport complete.", new TargetInfo(targetCenter, this.parent.Map), MessageTypeDefOf.PositiveEvent);
        }

        private void StartTacticalTargeting_In(List<Building> pads)
        {
            Find.Targeter.BeginTargeting(new TargetingParameters
            {
                canTargetLocations = true,
                canTargetPawns = true
            }, (LocalTargetInfo target) =>
            {
                ExecuteMultiBeam_In(pads, target);
            });
        }

        private void ExecuteMultiBeam_In(List<Building> pads, LocalTargetInfo target)
        {
            List<Pawn> potentialTargets = new List<Pawn>();
            if (target.HasThing && target.Thing is Pawn pTarget) potentialTargets.Add(pTarget);
            foreach (Pawn p in GenRadial.RadialDistinctThingsAround(target.Cell, this.parent.Map, 3.9f, true).OfType<Pawn>())
            {
                if (!potentialTargets.Contains(p) && (p.Faction == Faction.OfPlayer || p.IsPrisonerOfColony)) potentialTargets.Add(p);
            }

            int maxCapacity = pads.Count;
            List<Pawn> finalTargets = potentialTargets.Take(maxCapacity).ToList();

            if (finalTargets.NullOrEmpty())
            {
                Messages.Message("No valid targets locked within range.", this.parent, MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (!TryDrainPower(Props.tacticalBeamEnergyCost)) return;

            for (int i = 0; i < finalTargets.Count; i++)
            {
                Pawn p = finalTargets[i];
                Building pad = pads[i];
                IntVec3 padCell = TransporterPadUtil.GetPadCell(pad);

                TransporterVFX.PlayBeam(p.Map, p.Position);
                p.DeSpawn(DestroyMode.Vanish);
                GenSpawn.Spawn(p, padCell, this.parent.Map);
                TransporterVFX.PlayBeam(this.parent.Map, padCell);
                ApplyTransporterSickness(p);
            }
            Messages.Message($"Emergency extraction complete.", this.parent, MessageTypeDefOf.PositiveEvent);
        }

        private bool TryDrainPower(float amount)
        {
            CompPowerTrader consolePower = this.parent.GetComp<CompPowerTrader>();
            if (consolePower != null && consolePower.PowerNet != null)
            {
                if (consolePower.PowerNet.CurrentStoredEnergy() < amount)
                {
                    Messages.Message($"Insufficient energy reserves. Required: {amount} Wd", this.parent, MessageTypeDefOf.RejectInput, false);
                    return false;
                }
                DrainPowerFromNet(consolePower.PowerNet, amount);
                return true;
            }
            return false;
        }

        private void DrainPowerFromNet(PowerNet net, float amount)
        {
            if (net == null || net.batteryComps == null) return;
            float remaining = amount;
            foreach (CompPowerBattery battery in net.batteryComps)
            {
                if (remaining <= 0f) break;
                float draw = Mathf.Min(battery.StoredEnergy, remaining);
                battery.DrawPower(draw);
                remaining -= draw;
            }
        }

        private void ApplyTransporterSickness(Pawn p)
        {
            if (p.stances?.stunner != null)
                p.stances.stunner.StunFor(120, this.parent);
        }
    }
}