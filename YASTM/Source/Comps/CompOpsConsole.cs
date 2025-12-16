// Source/Comps/CompOpsConsole.cs 

using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace YASTM
{
    public class CompProperties_ForceFieldConsole : CompProperties
    {
        public string toggleLabelKey = "ST.ForceField.Toggle";
        public string toggleDescKey  = "ST.ForceField.Toggle.Desc";

        public CompProperties_ForceFieldConsole()
        {
            compClass = typeof(CompForceFieldConsole);
        }
    }

    public class CompForceFieldConsole : ThingComp
    {
        public CompProperties_ForceFieldConsole Props => (CompProperties_ForceFieldConsole)props;

        private bool fieldOn;
        private Sustainer humSustainer;

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref fieldOn, "ST_forceFieldOn", false);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            // Basiskram ausgeben
            foreach (var g in base.CompGetGizmosExtra())
                yield return g;


            var icon = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/ForceField", false)
                       ?? ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/ST_ForceField_Toggle", false);

            var cmd = new Command_Toggle
            {
                defaultLabel = Props.toggleLabelKey.Translate(),
                defaultDesc  = Props.toggleDescKey.Translate(),
                icon         = icon,
                isActive     = () => fieldOn,
                toggleAction = ToggleField
            };

            if (!CanUseNow(out var reason))
                cmd.Disable(reason);

            yield return cmd;
            yield break; 
        }

        private bool CanUseNow(out string reason)
        {
            reason = null;

            // Power & Flick
            var compPower = parent.TryGetComp<CompPowerTrader>();
            var compFlick = parent.TryGetComp<CompFlickable>();
            if ((compPower != null && !compPower.PowerOn) || (compFlick != null && !compFlick.SwitchIsOn))
            {
                reason = "ST.Common.NeedsPower".Translate();
                return false;
            }


            var af = parent.GetComp<CompAffectedByFacilities>();
            if (af == null || af.LinkedFacilitiesListForReading.Count == 0)
            {
                reason = "No linked force-field emitters.";
                return false;
            }

            bool anyEmitter = af.LinkedFacilitiesListForReading.Any(t => t.TryGetComp<CompForceFieldEmitter>() != null);
            if (!anyEmitter)
            {
                reason = "No linked force-field emitters.";
                return false;
            }

            return true;
        }

        private void ToggleField()
        {
            fieldOn = !fieldOn;


            SoundDef.Named("ST_SFX_LCARS_Click")?
                .PlayOneShot(new TargetInfo(parent.Position, parent.Map));


            var af = parent.GetComp<CompAffectedByFacilities>();
            if (af != null)
            {
                foreach (var t in af.LinkedFacilitiesListForReading)
                    t.TryGetComp<CompForceFieldEmitter>()?.SetActive(fieldOn, this);
            }


            if (fieldOn)
            {
                if (humSustainer == null)
                {
                    var snd = SoundDef.Named("ST_SFX_ForceFieldHum");
                    if (snd != null)
                    {
                        var info = SoundInfo.InMap(new TargetInfo(parent.Position, parent.Map));
                        humSustainer = snd.TrySpawnSustainer(info);
                    }
                }
            }
            else
            {
                humSustainer?.End();
                humSustainer = null;
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            humSustainer?.Maintain();
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            humSustainer?.End();
            humSustainer = null;
            base.PostDestroy(mode, previousMap);
        }
    }
}

