// File: Source/Quest/GameComponent_QuestWatchers.cs
using System.Collections.Generic;
using Verse;

namespace StarTrekFactions
{
    public class GameComponent_QuestWatchers : GameComponent
    {
        private readonly List<StarTrekFactions.QuestNodes.QuestPart_WaitEnemiesDefeated> waiters =
            new List<StarTrekFactions.QuestNodes.QuestPart_WaitEnemiesDefeated>();

        public GameComponent_QuestWatchers(Game game) { }

        public static GameComponent_QuestWatchers Instance
            => Current.Game?.GetComponent<GameComponent_QuestWatchers>();

        public void Register(StarTrekFactions.QuestNodes.QuestPart_WaitEnemiesDefeated part)
        {
            if (part == null) return;
            if (!waiters.Contains(part)) waiters.Add(part);
        }

        public void Unregister(StarTrekFactions.QuestNodes.QuestPart_WaitEnemiesDefeated part)
        {
            if (part == null) return;
            waiters.Remove(part);
        }

        public override void GameComponentTick()
        {
            if (waiters.Count == 0) return;
            // Alle 60 Ticks (≈ einmal/Sek.) prüfen
            if (Find.TickManager.TicksGame % 60 != 0) return;

            for (int i = waiters.Count - 1; i >= 0; i--)
            {
                var p = waiters[i];
                if (p == null) { waiters.RemoveAt(i); continue; }
                if (p.CheckAndMaybeComplete())
                {
                    waiters.RemoveAt(i);
                }
            }
        }
    }
}
