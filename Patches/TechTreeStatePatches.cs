using HarmonyLib;
using JetBrains.Annotations;

namespace BetterCoreManagement.Patches
{
    public static class TechTreeStatePatches
    {
        [HarmonyPatch(typeof(TechTreeState), nameof(TechTreeState.NumCoresOfTypePlaced))]
        public static class TechTreeStateNumCoresOfTypePlacedChanges
        {
            [UsedImplicitly]
            public static void Postfix(TechTreeState __instance, int typeIndex, ref int __result)
            {
                //Validate totalResearchCores have the right number of items.
                for (int j = 0; j < BetterCoreManagement.VirtualCoreCounts.Count; j++)
                {
                    if (BetterCoreManagement.VirtualCoreCounts[(ResearchCoreDefinition.CoreType)j] > 0)
                    {
                        if (j < __instance.totalResearchCores.Count)
                        {
                            continue;
                        }
                        
                        while (j >= __instance.totalResearchCores.Count)
                        {
                            __instance.totalResearchCores.Add(0);
                        }
                    }
                }
                
                var result = 0;
                if (__instance.totalResearchCores.Count <= typeIndex)
                    for (var i = 0; i < (typeIndex - __instance.totalResearchCores.Count); i++)
                        __instance.totalResearchCores.Add(0);
                else
                {
                    result = __instance.totalResearchCores[typeIndex] + BetterCoreManagement.VirtualCoreCounts[(ResearchCoreDefinition.CoreType)typeIndex];
                }

                __result = result;
            }
        }
    }
}