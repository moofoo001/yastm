using RimWorld;
using Verse;

namespace YASTM
{
    // 1. Die "Eigenschaften" (Properties) für das XML
    public class HediffCompProperties_Hologram : HediffCompProperties
    {
        public HediffCompProperties_Hologram()
        {
            this.compClass = typeof(HediffComp_Hologram);
        }
    }

    // 2. Die Logik (Was passiert im Spiel)
    public class HediffComp_Hologram : HediffComp
    {
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            Pawn pawn = this.Pawn;
            if (pawn == null) return;

            // Wenn das Hologramm stirbt oder auch nur bewusstlos wird (Downed)...
            if (pawn.Dead || pawn.Downed)
            {
                // ... löse es auf!
                DissipateHologram(pawn);
            }
        }

        private void DissipateHologram(Pawn pawn)
        {
            if (pawn.Map != null)
            {
                // Visueller Effekt: Ein kleiner Blitz/Leuchten beim Verschwinden
                FleckMaker.ThrowLightningGlow(pawn.TrueCenter(), pawn.Map, 1.0f);
            }

            // WICHTIG: Pawn komplett löschen (keine Leiche, kein Loot)
            pawn.Destroy(DestroyMode.Vanish);
        }
    }
}