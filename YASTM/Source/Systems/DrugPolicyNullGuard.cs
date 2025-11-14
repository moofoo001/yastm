// Source/Systems/DrugPolicyNullGuard.cs
using HarmonyLib;
using Verse;

namespace YASTM.Systems
{
    [StaticConstructorOnStartup]
    public static class DrugPolicyNullGuard
    {
        static DrugPolicyNullGuard()
        {

            var _ = new Harmony("YASTM.Systems.DrugPolicyNullGuard"); 
        }
    }
}
