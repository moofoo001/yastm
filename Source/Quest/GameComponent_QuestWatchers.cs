// File: Source/Quest/GameComponent_QuestWatchers.cs
using System.Collections.Generic;
using Verse;
using StarTrekFactions.QuestNodes;

namespace StarTrekFactions
{
    public class GameComponent_QuestWatchers : GameComponent
    {
        private readonly List<QuestPart_WaitEnemiesDefeated> waiters =
            new List<QuestPart_WaitEnemiesDefeated>();
        private readonly List<QuestPart_DelayThenSignalOnSignal> timers =
            new List<QuestPart_DelayThenSignalOnSignal>();

        public GameComponent_QuestWatchers(Game game) { }

        public static GameComponent_QuestWatchers Instance
            => Current.Game?.GetComponent<GameComponent_QuestWatchers>();

        // Registrierungen
        public void Register(QuestPart_WaitEnemiesDefeated part)
        {
            if (part != null && !waiters.Contains(part)) waiters.Add(part);
        }

        public void Register(QuestPart_DelayThenSignalOnSignal part)
        {
            if (part != null && !timers.Contains(part)) timers.Add(part);
        }

        public override void GameComponentTick()
        {
            int tick = Find.TickManager.TicksGame;

            // Waiter prüfen ~ jede Sekunde
            if (waiters.Count > 0 && tick % 60 == 0)
            {
                for (int i = waiters.Count - 1; i >= 0; i--)
                {
                    var p = waiters[i];
                    if (p == null) { waiters.RemoveAt(i); continue; }
                    if (p.CheckAndMaybeComplete()) waiters.RemoveAt(i);
                }
            }

            // Timer prüfen ~ jede Sekunde
            if (timers.Count > 0 && tick % 60 == 0)
            {
                for (int i = timers.Count - 1; i >= 0; i--)
                {
                    var p = timers[i];
                    if (p == null) { timers.RemoveAt(i); continue; }
                    if (p.TickAndMaybeFire()) timers.RemoveAt(i);
                }
            }
        }
    }
}
