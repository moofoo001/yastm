using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace YASTM
{
    public class CompProperties_Holodeck : CompProperties
    {
        public float trainingXpPerTick = 0.15f;
        public float injuryChance = 0.001f;
        public float powerTrainingMode = 2000f;
        public float powerRelaxMode = 500f;
    
        // 240 Ticks = 4 seconds
        public int holoRefreshInterval = 240; 

        public CompProperties_Holodeck()
        {
            this.compClass = typeof(CompHolodeck);
        }
    }

    public class CompHolodeck : ThingComp
    {
        public CompProperties_Holodeck Props => (CompProperties_Holodeck)this.props;

        private CompPowerTrader powerComp;
        private bool isTrainingMode = false;
        
        // Hologramm Timer
        private int nextHoloTick = 0;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            powerComp = this.parent.GetComp<CompPowerTrader>();
            // Initialisiere Timer
            nextHoloTick = Find.TickManager.TicksGame + 10;
        }

        public override void CompTick()
        {
            base.CompTick();

            bool hasPower = (powerComp != null && powerComp.PowerOn);
            if (!hasPower) return;


            if (parent.IsHashIntervalTick(60))
            {
                CheckForUsers();
                
                // set power consumption
                powerComp.PowerOutput = isTrainingMode 
                    ? -Props.powerTrainingMode 
                    : -Props.powerRelaxMode;
            }

            // Hologramm Timer
            if (Find.TickManager.TicksGame >= nextHoloTick)
            {
                SpawnHoloFleck();
                nextHoloTick = Find.TickManager.TicksGame + Props.holoRefreshInterval;
            }
        }

        private void SpawnHoloFleck()
        {
            if (parent.Map == null) return;

            // fleck def based on mode
            string defName = isTrainingMode ? "ST_Holo_Training" : "ST_Holo_Risa";

            FleckDef holoDef = DefDatabase<FleckDef>.GetNamedSilentFail(defName);

            if (holoDef != null)
            {

                FleckMaker.Static(parent.TrueCenter(), parent.Map, holoDef);
            }
            else
            {
                // warning only occasionally to avoid log spam
                 if (Find.TickManager.TicksGame % 600 == 0)
                    Log.Warning($"[YASTM] CompHolodeck: Could not find FleckDef named '{defName}'. Check ST_Holo_Flecks.xml!");
            }
        }

        private void CheckForUsers()
        {
            // Standard: Relax Mode
            isTrainingMode = false;

            if (!parent.Spawned) return;
            
            IntVec3 cell = parent.InteractionCell;
            List<Thing> thingList = cell.GetThingList(parent.Map);
            
            foreach (Thing t in thingList)
            {
                if (t is Pawn p && !p.Dead && !p.Downed)
                {
                    // Training Mode
                    if (p.CurJobDef == JobDefOf.DoBill)
                    {
                        isTrainingMode = true;
                        return; // early exit
                    }
                }
            }
        }
    }
}