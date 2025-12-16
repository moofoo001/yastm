using System.Collections.Generic;
using Verse;
// WICHTIG: Hier laden wir unsere Quest-Klassen
using YASTM.Source.Quest; 

// FIX: Namespace angepasst (war StarTrekFactions)
namespace YASTM.Source.Systems
{
    public class GameComponent_QuestWatchers : GameComponent
    {
        private const bool LogDebug = true;
        private int _lastLogTick;
    
        private readonly List<QuestPart_WaitEnemiesDefeated> waiters =
            new List<QuestPart_WaitEnemiesDefeated>();
        private readonly List<QuestPart_DelayThenSignalOnSignal> timers =
            new List<QuestPart_DelayThenSignalOnSignal>();

        public bool ObeliskAssignmentUsed;

        public GameComponent_QuestWatchers(Game game)
        {
            if (LogDebug) Log.Message("[YASTM][Watcher] GameComponent_QuestWatchers constructed.");
        }

        public static GameComponent_QuestWatchers Instance
        {
            get
            {
                var game = Current.Game;
                if (game == null) return null;

                var comp = game.GetComponent<GameComponent_QuestWatchers>();
                if (comp == null)
                {
                    comp = new GameComponent_QuestWatchers(game);
                    game.components?.Add(comp);
                    Log.Message("[YASTM][Watcher] Injected GameComponent_QuestWatchers at runtime.");
                }
                return comp;
            }
        }

        public void Register(QuestPart_WaitEnemiesDefeated part)
        {
            if (part != null && !waiters.Contains(part))
            {
                waiters.Add(part);
                if (LogDebug) Log.Message("[YASTM][Watcher] +Waiter (count=" + waiters.Count + ")");
            }
        }

        public void Register(QuestPart_DelayThenSignalOnSignal part)
        {
            if (part != null && !timers.Contains(part))
            {
                timers.Add(part);
                if (LogDebug) Log.Message("[YASTM][Watcher] +Timer  (count=" + timers.Count + ")");
            }
        }

        public override void GameComponentTick()
        {
            int tick = Find.TickManager.TicksGame;

            if (waiters.Count > 0 && tick % 60 == 0)
            {
                for (int i = waiters.Count - 1; i >= 0; i--)
                {
                    var p = waiters[i];
                    if (p == null) { waiters.RemoveAt(i); continue; }
                    if (p.CheckAndMaybeComplete())
                    {
                        waiters.RemoveAt(i);
                        if (LogDebug) Log.Message("[YASTM][Watcher] -Waiter (count=" + waiters.Count + ")");
                    }
                }
            }

            if (timers.Count > 0 && tick % 60 == 0)
            {
                for (int i = timers.Count - 1; i >= 0; i--)
                {
                    var p = timers[i];
                    if (p == null) { timers.RemoveAt(i); continue; }
                    if (p.TickAndMaybeFire(60))
                    {
                        timers.RemoveAt(i);
                        if (LogDebug) Log.Message("[YASTM][Watcher] -Timer  (count=" + timers.Count + ")");
                    }
                }
            }

            if (LogDebug && tick - _lastLogTick >= 120 && (waiters.Count > 0 || timers.Count > 0))
            {
                _lastLogTick = tick;
                Log.Message($"[YASTM][Watcher] tick={tick} waiters={waiters.Count} timers={timers.Count}");
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ObeliskAssignmentUsed, "YASTM_ObeliskAssignmentUsed", false);
        }
    }
}