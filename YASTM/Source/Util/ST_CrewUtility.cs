using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace YASTM
{
    public static class ST_CrewUtility
    {
        public static IEnumerable<Pawn> GetAllActiveCrewMembers()
        {
            // is a crew member?
            if (PawnsFinder.AllMaps_FreeColonists != null)
            {
                foreach (var pawn in PawnsFinder.AllMaps_FreeColonists)
                {
                    yield return pawn;
                }
            }

            // caravan crew members
            if (Find.World != null && Find.WorldObjects != null)
            {
                foreach (var caravan in Find.WorldObjects.Caravans)
                {
                    foreach (var pawn in caravan.PawnsListForReading)
                    {
                        if (pawn.IsFreeColonist)
                        {
                            yield return pawn;
                        }
                    }
                }

                // other world objects
                foreach (var worldObj in Find.WorldObjects.AllWorldObjects)
                {
                    // skip non-container objects
                    if (worldObj is Caravan) continue;
                    if (worldObj is MapParent) continue; // skip maps
                    if (worldObj.def.defName == "DestroyedSettlement") continue;

                    // check for thing holder
                    if (worldObj is IThingHolder holder)
                    {
                        ThingOwner container = holder.GetDirectlyHeldThings();
                        if (container != null)
                        {
                            foreach (var thing in container)
                            {
                                if (thing is Pawn pawn && pawn.IsFreeColonist)
                                {
                                    yield return pawn;
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void DistributeStarfleetRations()
        {
            foreach (var crew in GetAllActiveCrewMembers())
            {
                if (crew.needs?.food != null)
                {
                    crew.needs.food.CurLevel += 0.1f;
                }
            }
        }
    }
}