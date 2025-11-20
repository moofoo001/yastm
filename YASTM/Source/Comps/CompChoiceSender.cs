using System.Collections.Generic;
using System; 
using RimWorld;
using Verse;
using Verse.Sound;

namespace StarTrekFactions.Comps
{
    public class ChoiceOption
    {
        public string labelKey;
        public string outSignal;

        public bool sendLetter;
        public string letterLabelKey;
        public string letterTextKey;
        public LetterDef letterDef;

        public bool fireIncident;
        public IncidentDef incidentDef;
        public float pointsFactor = 1f;
    }

    public class CompProperties_ChoiceSender : CompProperties
    {
        public List<ChoiceOption> options = new List<ChoiceOption>();
        public bool requirePowerOn;

        public CompProperties_ChoiceSender()
        {
            compClass = typeof(CompChoiceSender);
        }
    }

    public class CompChoiceSender : ThingComp
    {
        public CompProperties_ChoiceSender Props => (CompProperties_ChoiceSender)props;

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent.Faction != Faction.OfPlayer) yield break;

            if (Props.requirePowerOn)
            {
                var p = parent.TryGetComp<CompPowerTrader>();
                if (p != null && !p.PowerOn) yield break;
            }

            if (Props.options == null) yield break;

            foreach (var opt in Props.options)
            {
                // 
                string label = opt.labelKey.NullOrEmpty()
                    ? "Choose"
                    : opt.labelKey.Translate().ToString();

                yield return new Command_Action
                {
                    defaultLabel = label,
                    defaultDesc  = label,
                    icon = null,
                    action = () =>
                    {

                        if (!opt.outSignal.NullOrEmpty())
                        {
                            Find.SignalManager.SendSignal(new Signal(opt.outSignal, new NamedArgument(parent, "SOURCE")));
                        }


                        if (opt.sendLetter && opt.letterDef != null)
                        {
                            TaggedString lab = opt.letterLabelKey.NullOrEmpty() ? "Starfleet" : opt.letterLabelKey.Translate();
                            TaggedString txt = opt.letterTextKey.NullOrEmpty() ? "" : opt.letterTextKey.Translate();

 
                            try
                            {
                                Find.LetterStack.ReceiveLetter(lab, txt, opt.letterDef);
                            }
                            catch (Exception e)
                            {
                                Log.Warning($"[STF] ChoiceSender Letter failed ({opt.letterDef?.defName}): {e.Message}");
                            }
                        }

                        if (opt.sendLetter && opt.letterDef != null)
                        {
                            string l = !opt.letterLabelKey.NullOrEmpty() ? opt.letterLabelKey.Translate().ToString() : "Transmission";
                            string t = !opt.letterTextKey.NullOrEmpty() ? opt.letterTextKey.Translate().ToString() : string.Empty;
                            Find.LetterStack.ReceiveLetter(l, t, opt.letterDef, parent);
                        }


                        if (opt.fireIncident && parent.Map != null && opt.incidentDef != null)
                        {
                            var map = parent.Map;
                            float basePts = StorytellerUtility.DefaultThreatPointsNow(map);
                            float factor  = opt.pointsFactor <= 0f ? 1f : opt.pointsFactor;
                            float points  = basePts * factor;

                            var parms = StorytellerUtility.DefaultParmsNow(opt.incidentDef.category, map);
                            parms.points = points;
                            parms.forced = true;

                            if (!opt.incidentDef.Worker.TryExecute(parms))
                            {
                                Log.Warning($"[STF] ChoiceSender: Incident '{opt.incidentDef.defName}' konnte nicht ausgelöst werden.");
                            }
                        }

                        SoundDefOf.Click.PlayOneShotOnCamera();
                    }
                };
            }
        }
    }
}

