using UnityEngine;
using Verse;
using RimWorld;
using System.Collections.Generic;

namespace YASTM
{
    // get the fuel from "ST_ReplicatorFeedstock" items on the ground
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
            
            //every second
            if (parent.IsHashIntervalTick(60) && tank != null)
            {
                // if full, skip
                if (tank.Fuel >= tank.Props.fuelCapacity) return;

                AbsorbFeedstock();
            }
        }

        private void AbsorbFeedstock()
        {
            //
            var cells = new List<IntVec3> { parent.Position, parent.InteractionCell };

            foreach (var cell in cells)
            {
                var thingList = cell.GetThingList(parent.Map);
                for (int i = thingList.Count - 1; i >= 0; i--)
                {
                    Thing t = thingList[i];
                    
                    // is it feedstock?
                    if (t.def.defName == "ST_ReplicatorFeedstock")
                    {
                        // check how much we can take
                        float space = tank.Props.fuelCapacity - tank.Fuel;
                        int countToTake = Mathf.Min(t.stackCount, (int)space);

                        if (countToTake > 0)
                        {
                            // insert into tank
                            tank.Refuel(countToTake);
                            
                            // show text
                            MoteMaker.ThrowText(parent.DrawPos, parent.Map, "+" + countToTake, Color.green);
                            
                            // remove from ground
                            if (countToTake >= t.stackCount) t.Destroy();
                            else t.stackCount -= countToTake;
                        }
                    }
                }
            }
        }
    }
}