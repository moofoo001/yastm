using UnityEngine;
using Verse;

[StaticConstructorOnStartup]
public static class CharacterCardUtilityUIPatch
{
    public static readonly Texture2D ColorPawn_Icon
        = ContentFinder<Texture2D>.Get("UI/Icons/Starfleet/Placeholder", true);
}

[StaticConstructorOnStartup]
public static class SmartColorWidgets
{
    public static readonly Texture2D BrightnessTexture;

    static SmartColorWidgets()
    {
        BrightnessTexture = new Texture2D(256, 1, TextureFormat.RGBA32, false);

        BrightnessTexture.Apply();
    }
}
