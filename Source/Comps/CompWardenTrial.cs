// File: Source/Comps/CompWardenTrial.cs
using System;
using System.Collections.Generic; // <- für List<T>, IEnumerable<T>
using RimWorld;
using Verse;

namespace StarTrekFactions.Comps
{
    public class WardenCheckpoint
    {
        public int atTicks;
        public IncidentDef incidentDef;
        public float pointsFactor = 0.5f;

        
        [Unsaved(false)] public bool fired = false;

        public WardenCheckpoint() { }
    }

    public class CompProperties_WardenTrial : CompProperties
    {
        public bool requirePowerOn = true;

        
        public int durationTicks = 15000;

        
        public string outSignalStarted;
        public string outSignalSucceeded;
        public string outSignalFailed;

        
        public List<WardenCheckpoint> checkpoints = new List<WardenCheckpoint>();

        public CompProperties_WardenTrial()
        {
            compClass = typeof(CompWardenTrial);
        }
    }

    public class CompWardenTrial : ThingComp
    {
        public CompProperties_WardenTrial Props => (CompProperties_WardenTrial)props;

        private bool active;
        private int ticksOnline;            
        private bool successOrFailSent;
        private bool unlocked;


public override void ReceiveCompSignal(string signal)
{
    if (signal == "STQ.Obelisks.Warden.Unlock")
    {
        unlocked = true;
    }
}

        
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent.Faction != Faction.OfPlayer) yield break;
            if (!unlocked) yield break;                 // <— neu

            if (!active && !successOrFailSent)
            {
                yield return new Command_Action
                {
                    defaultLabel = "Initialize Warden Trial",
                    defaultDesc  = "Start the subspace lock. Keep this unit powered until completion.",
                    action = () =>
                    {
                        active = true;
                        ticksOnline = 0;
                        if (Props.checkpoints != null)
                            for (int i = 0; i < Props.checkpoints.Count; i++) Props.checkpoints[i].fired = false;

                        if (!Props.outSignalStarted.NullOrEmpty())
                            Find.SignalManager.SendSignal(new Signal(Props.outSignalStarted, new NamedArgument(parent, "SOURCE")));
                    }
                };
            }
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            if (!active || successOrFailSent) return;

            
            if (Props.requirePowerOn)
            {
                var p = parent.TryGetComp<CompPowerTrader>();
                if (p != null && !p.PowerOn)
                {
                    
                    return;
                }
            }

            
            ticksOnline += 250;

            
            if (Props.checkpoints != null && parent.Map != null)
            {
                for (int i = 0; i < Props.checkpoints.Count; i++)
                {
                    var cp = Props.checkpoints[i];
                    if (!cp.fired && ticksOnline >= cp.atTicks && cp.incidentDef != null)
                    {
                        FireIncident(cp);
                        cp.fired = true;
                    }
                }
            }

            
            if (ticksOnline >= Props.durationTicks)
            {
                successOrFailSent = true;
                active = false;
                if (!Props.outSignalSucceeded.NullOrEmpty())
                    Find.SignalManager.SendSignal(new Signal(Props.outSignalSucceeded, new NamedArgument(parent, "SOURCE")));
            }
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            
            if (active && !successOrFailSent)
            {
                successOrFailSent = true;
                active = false;
                if (!Props.outSignalFailed.NullOrEmpty())
                    Find.SignalManager.SendSignal(new Signal(Props.outSignalFailed, new NamedArgument(parent, "SOURCE")));
            }
        }

        private void FireIncident(WardenCheckpoint cp)
        {
            var map = parent.Map;
            float basePts = StorytellerUtility.DefaultThreatPointsNow(map);
            float factor  = cp.pointsFactor <= 0f ? 0.5f : cp.pointsFactor;
            float points  = Math.Max(35f, basePts * factor); // 1.6: konservative Untergrenze

            var parms = StorytellerUtility.DefaultParmsNow(cp.incidentDef.category, map);
            parms.points = points;
            parms.forced = true;

            if (!cp.incidentDef.Worker.TryExecute(parms))
            {
                Log.Warning($"[STF] WardenTrial: Incident '{cp.incidentDef.defName}' konnte nicht ausgelöst werden.");
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref active, "STF_WardenTrial_active");
            Scribe_Values.Look(ref ticksOnline, "STF_WardenTrial_ticksOnline");
            Scribe_Values.Look(ref successOrFailSent, "STF_WardenTrial_done");

            
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                if (Props.checkpoints != null)
                {
                    for (int i = 0; i < Props.checkpoints.Count; i++)
                    {
                        bool f = Props.checkpoints[i].fired;
                        Scribe_Values.Look(ref f, $"STF_WardenTrial_cpFired_{i}");
                    }
                }
            }
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                if (Props.checkpoints != null)
                {
                    for (int i = 0; i < Props.checkpoints.Count; i++)
                    {
                        bool f = false;
                        Scribe_Values.Look(ref f, $"STF_WardenTrial_cpFired_{i}");
                        Props.checkpoints[i].fired = f;
                    }
                }
            }
        }
    }
}
