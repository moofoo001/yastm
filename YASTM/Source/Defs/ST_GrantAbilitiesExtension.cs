using System.Collections.Generic;
using Verse;
using RimWorld;

namespace YASTM
{
    // Diese Extension erlaubt es uns, Abilities direkt an einem TraitDef zu definieren.
    public class ST_GrantAbilitiesExtension : DefModExtension
    {
        // Liste der Fähigkeiten, die dieser Trait gewährt
        public List<AbilityDef> abilities = new List<AbilityDef>();
    }
}