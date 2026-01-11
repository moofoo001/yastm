using RimWorld;
using Verse;
using System.Linq;
using StarTrekFactions;

namespace YASTM.Relics
{
    /// <summary>
    /// Tracks whether the player's faction currently owns the Sword of Kahless.
    /// </summary>
    public class GameComponent_KahlessRelic : GameComponent
    {
        public bool swordOfKahlessOwned;

        public GameComponent_KahlessRelic(Game game) : base()
        {
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref swordOfKahlessOwned, "swordOfKahlessOwned", false);
        }

        public override void GameComponentTick()
        {
            // Check every 2500 ticks (approx. every 41.6 seconds)
            if (Find.TickManager.TicksGame % 2500 != 0)
                return;

            swordOfKahlessOwned = CheckSwordOwned();
        }

            private bool CheckSwordOwned()
            {
                // SwordDef via DefOf
                var swordDef = STFDefOf.ST_SwordOfKahless;
                if (swordDef == null)
                    return false;

                foreach (var map in Find.Maps)
                {
                    // is the sword present in this map?
                    var pawns = map.mapPawns.FreeColonistsSpawned;
                    foreach (var pawn in pawns)
                    {
                        var eq = pawn.equipment?.Primary;
                        if (eq != null && eq.def == swordDef)
                            return true;
                    }

                    // check for stored swords
                    var things = map.listerThings.ThingsOfDef(swordDef);
                    foreach (var t in things)
                    {
                        if (t.Faction == Faction.OfPlayer)
                            return true;

                        if (t.IsInAnyStorage())
                            return true;
                    }
                }

                return false;
            }

        public static GameComponent_KahlessRelic Instance
        {
            get
            {
                return Current.Game?.GetComponent<GameComponent_KahlessRelic>();
            }
        }
    }
}
