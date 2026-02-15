using RimWorld;
using Verse;
using UnityEngine;

namespace YASTM
{
    public class CompProperties_AbilityShapeshift : CompProperties_AbilityEffect
    {
        public CompProperties_AbilityShapeshift()
        {
            this.compClass = typeof(CompAbilityEffect_Shapeshift);
        }
    }

    public class CompAbilityEffect_Shapeshift : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            
            Pawn caster = parent.pawn;
            Pawn victim = target.Pawn;

            if (caster == null || victim == null) return;
            if (!victim.RaceProps.Humanlike) 
            {
                Messages.Message("Can only mimic humanoids!", MessageTypeDefOf.RejectInput, false);
                return;
            }

            // 1. Effekt: Rauch/Nebel
            FleckMaker.ThrowSmoke(caster.Position.ToVector3Shifted(), caster.Map, 2.0f);
            
            // 2. KOPIEREN DES AUSSEHENS (Story Data)
            caster.story.hairDef = victim.story.hairDef;
            
            // KORREKTUR: "HairColor" muss großgeschrieben werden!
            caster.story.HairColor = victim.story.HairColor;
            
            caster.story.bodyType = victim.story.bodyType;
            caster.story.headType = victim.story.headType;
            
            // Hautfarbe übernehmen
            caster.story.skinColorOverride = victim.story.SkinColor;
            
            // Bart & Tattoos (Falls vorhanden)
            if (victim.style != null && caster.style != null)
            {
                caster.style.beardDef = victim.style.beardDef;
                caster.style.FaceTattoo = victim.style.FaceTattoo;
                caster.style.BodyTattoo = victim.style.BodyTattoo;
            }

            // 3. Grafik-Refresh erzwingen
            caster.Drawer.renderer.SetAllGraphicsDirty();
            PortraitsCache.SetDirty(caster);
            
            Messages.Message($"{caster.LabelShort} has shifted form to mimic {victim.LabelShort}!", MessageTypeDefOf.NeutralEvent, false);
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            return target.Pawn != null && target.Pawn.RaceProps.Humanlike;
        }
    }
}