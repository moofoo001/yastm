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
            // 1. Kolonisten auf allen Karten (Maps)
            // Dies ist die Standard-Liste und sehr schnell.
            if (PawnsFinder.AllMaps_FreeColonists != null)
            {
                foreach (var pawn in PawnsFinder.AllMaps_FreeColonists)
                {
                    yield return pawn;
                }
            }

            // 2. Kolonisten in Karawanen (Caravans)
            // Karawanen sind eine spezifische, stabile Klasse.
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

                // 3. Kolonisten in Transportkapseln & Shuttles (Generischer Ansatz)
                // FIX: Statt nach dem Typ "TravelingTransportPods" zu suchen (der den Fehler CS0246 verursacht),
                // suchen wir nach allen Welt-Objekten, die Dinge beinhalten (IThingHolder),
                // aber keine Karawanen (schon erledigt) oder Maps (Siedlungen) sind.
                foreach (var worldObj in Find.WorldObjects.AllWorldObjects)
                {
                    // Wir überspringen, was wir schon kennen oder was irrelevant ist
                    if (worldObj is Caravan) continue;
                    if (worldObj is MapParent) continue; // Siedlungen, Außenposten etc.
                    if (worldObj.def.defName == "DestroyedSettlement") continue;

                    // Prüfen: Ist es ein Container? (Kapseln implementieren IThingHolder)
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