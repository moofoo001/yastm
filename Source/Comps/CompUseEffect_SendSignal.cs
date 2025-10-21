using System.Collections.Generic;
using RimWorld;
using Verse;

namespace StarTrekFactions.Comps
{
    
    public class CompProperties_UseEffect_SendSignal : CompProperties_UseEffect
    {
        public bool requirePowerOn = false;

        
        public string inSignal;

        
        public bool sendLetter = false;
        public LetterDef letterDef;
        public string letterLabelKey;
        public string letterTextKey;

        
        public bool fireIncidentOnUse = false;
        public IncidentDef incidentDef;
        public float pointsFactor = 1f;

        
        public class SpawnOnUseEntry
        {
            public ThingDef thingDef;
            public int count = 1; 
        }
        public List<SpawnOnUseEntry> spawnOnUse;

        public CompProperties_UseEffect_SendSignal()
        {
            compClass = typeof(CompUseEffect_SendSignal);
        }
    }

    public class CompUseEffect_SendSignal : CompUseEffect
    {
        public CompProperties_UseEffect_SendSignal Props => (CompProperties_UseEffect_SendSignal)props;

        public override void DoEffect(Pawn usedBy)
        {
            
            if (Props.requirePowerOn)
            {
                var power = parent.TryGetComp<CompPowerTrader>();
                if (power != null && !power.PowerOn)
                {
                    Messages.Message("ST.MustBePowered".Translate(), parent, MessageTypeDefOf.RejectInput, false);
                    return;
                }
            }

            base.DoEffect(usedBy);

            
            if (Props.sendLetter && Props.letterDef != null)
            {
                string label = !Props.letterLabelKey.NullOrEmpty() ? Props.letterLabelKey.Translate() : "Starfleet communiqué".Translate();
                string text  = !Props.letterTextKey.NullOrEmpty()  ? Props.letterTextKey.Translate()  : string.Empty;
                Find.LetterStack.ReceiveLetter(label, text, Props.letterDef);
            }

            
            if (!Props.inSignal.NullOrEmpty())
            {
                Find.SignalManager.SendSignal(new Signal(Props.inSignal));
            }

            
            if (Props.spawnOnUse != null && parent.Map != null)
            {
                foreach (var e in Props.spawnOnUse)
                {
                    if (e == null || e.thingDef == null) continue;
                    int c = e.count <= 0 ? 1 : e.count;
                    for (int i = 0; i < c; i++)
                    {
                        var thing = ThingMaker.MakeThing(e.thingDef);
                        GenPlace.TryPlaceThing(thing, parent.Position, parent.Map, ThingPlaceMode.Near);
                    }
                }
            }

            
            if (Props.fireIncidentOnUse && Props.incidentDef != null && parent.Map != null)
            {
                var parms = StorytellerUtility.DefaultParmsNow(Props.incidentDef.category, parent.Map);
                if (Props.pointsFactor > 0f) parms.points *= Props.pointsFactor;
                Props.incidentDef.Worker.TryExecute(parms);
            }
        }
    }
}
