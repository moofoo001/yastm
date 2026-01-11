using System.Collections.Generic;
using Verse;
using RimWorld;

namespace YASTM
{
    //ability def extension to grant abilities via traits
    public class ST_GrantAbilitiesExtension : DefModExtension
    {
        // List of abilities to grant
        public List<AbilityDef> abilities = new List<AbilityDef>();
    }
}