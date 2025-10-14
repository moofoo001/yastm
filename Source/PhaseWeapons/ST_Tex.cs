using UnityEngine;
using Verse;

namespace ST.PhaseWeapons
{
    // Lädt/erzeugt ALLE Texturen im Main Thread.
    [StaticConstructorOnStartup]
    public static class ST_Tex
    {
   public static readonly Texture2D PhaserPulse;
        public static readonly Texture2D PhaserPulse_Stun;
        public static readonly Texture2D PhaserPulse_Overcharge;
        public static readonly Texture2D BrightnessTexture;

        static ST_Tex()
        {
            PhaserPulse           = ContentFinder<Texture2D>.Get("Things/Projectile/PhaserPulse", false)           ?? BaseContent.BadTex;
            PhaserPulse_Stun      = ContentFinder<Texture2D>.Get("Things/Projectile/PhaserPulse_Stun", false)      ?? BaseContent.BadTex;
            PhaserPulse_Overcharge= ContentFinder<Texture2D>.Get("Things/Projectile/PhaserPulse_Overcharge", false)?? BaseContent.BadTex;
            BrightnessTexture = new Texture2D(256, 1, TextureFormat.RGBA32, false);
            for (int x = 0; x < 256; x++)
            {
                float v = x / 255f;
                BrightnessTexture.SetPixel(x, 0, new Color(v, v, v, 1f));
            }
            BrightnessTexture.Apply();
        }
         
        // ← NEU: kleines Helper für die Gizmo-Icons
        public static Texture2D IconFor(PhaserFireMode mode)
        {
            switch (mode)
            {
                case PhaserFireMode.Stun:       return PhaserPulse_Stun;
                case PhaserFireMode.Overcharge: return PhaserPulse_Overcharge;
                default:                        return PhaserPulse; // Kill/Lethal
            }
        }
    }
}
