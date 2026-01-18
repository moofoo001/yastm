using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;

namespace YASTM.Abilities
{
    public class CompAbilityEffect_CommandPresence : CompAbilityEffect
    {
        private const float Radius = 6f;                 // AoE
        private const int Duration = 2200;               // ~36s
        private HediffDef _buff;

        public override void Initialize(AbilityCompProperties props)
        {
            base.Initialize(props);
            _buff = DefDatabase<HediffDef>.GetNamedSilentFail("ST_CommandPresenceBuff");
        }

        public override bool GizmoDisabled(out string reason)
        {
            reason = null;
            return parent.pawn?.Spawned != true;
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (_buff == null) return;
            var caster = parent.pawn;
            if (caster?.Map == null) return;

            foreach (var cell in GenRadial.RadialCellsAround(caster.Position, Radius, useCenter: true))
            {
                if (!cell.InBounds(caster.Map)) continue;
                var p = cell.GetFirstPawn(caster.Map);
                if (p == null || p.Dead) continue;
                if (p.Faction != caster.Faction) continue;

                var h = p.health?.AddHediff(_buff);
                var comp = h?.TryGetComp<HediffComp_Disappears>();
                if (comp != null) comp.ticksToDisappear = Duration;

                // Sound
                var snd = DefDatabase<SoundDef>.GetNamedSilentFail("ST_SND_CommandPresenceCast");
                if (snd != null && caster?.Map != null)
                {
                    Verse.Sound.SoundStarter.PlayOneShot(snd, Verse.Sound.SoundInfo.InMap(new TargetInfo(caster.Position, caster.Map)));
                }
                
                FleckMaker.Static(caster.Position, caster.Map, FleckDefOf.ExplosionFlash, 1.3f);
                
                for (int i = 0; i < 5; i++)
                    FleckMaker.ThrowDustPuff(caster.Position, caster.Map, 1.5f);
            }
        }
    }
}

