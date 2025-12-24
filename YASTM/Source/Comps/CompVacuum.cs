using UnityEngine;
using Verse;
using RimWorld;
using System.Collections.Generic;

namespace YASTM
{
    // Dieser Chip kommt einfach ZUSÄTZLICH in das Gebäude
    public class CompVacuum : ThingComp
    {
        private CompRefuelable tank;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            tank = parent.TryGetComp<CompRefuelable>();
        }

        public override void CompTick()
        {
            base.CompTick();
            
            // Nur 1x pro Sekunde prüfen (60 Ticks), spart CPU
            if (parent.IsHashIntervalTick(60) && tank != null)
            {
                // Wenn Tank voll ist, nichts tun
                if (tank.Fuel >= tank.Props.fuelCapacity) return;

                AbsorbFeedstock();
            }
        }

        private void AbsorbFeedstock()
        {
            // Wir prüfen den Boden unter dem Gebäude und der Interaktions-Zelle
            var cells = new List<IntVec3> { parent.Position, parent.InteractionCell };

            foreach (var cell in cells)
            {
                var thingList = cell.GetThingList(parent.Map);
                for (int i = thingList.Count - 1; i >= 0; i--)
                {
                    Thing t = thingList[i];
                    
                    // Ist es unser Feedstock?
                    if (t.def.defName == "ST_ReplicatorFeedstock")
                    {
                        // Wie viel passt noch rein?
                        float space = tank.Props.fuelCapacity - tank.Fuel;
                        int countToTake = Mathf.Min(t.stackCount, (int)space);

                        if (countToTake > 0)
                        {
                            // Rein damit!
                            tank.Refuel(countToTake);
                            
                            // Effekt anzeigen
                            MoteMaker.ThrowText(parent.DrawPos, parent.Map, "+" + countToTake, Color.green);
                            
                            // Item verkleinern oder zerstören
                            if (countToTake >= t.stackCount) t.Destroy();
                            else t.stackCount -= countToTake;
                        }
                    }
                }
            }
        }
    }
}