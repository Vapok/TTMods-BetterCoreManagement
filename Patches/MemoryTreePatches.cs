using HarmonyLib;
using JetBrains.Annotations;

namespace BetterCoreManagement.Patches
{
    public static class MemoryTreePatches
    {
        [HarmonyPatch(typeof(MemoryTreeMachineList), nameof(MemoryTreeMachineList.UpdateAll))]
        public static class MemoryTreeMachineListUpdateAllChanges
        {
            [UsedImplicitly]
            public static void Postfix(MemoryTreeMachineList __instance)
            {
                if (__instance.curCount < 1)
                    return;

                for (var i = 0; i < __instance.curCount; i++)
                {
                    if (__instance.myArray[i].pendingCoreCount < 1 &&
                        __instance.myArray[i].GetErrorState() == MemoryTreeInstance.ErrorState.Full &&
                        __instance.myArray[i].FindCoreInInventory(out var coreLocation, out var researchCoreDefinition)
                       )
                    {
                        BetterCoreManagement.VirtualCoreCounts[researchCoreDefinition.coreType]++;
                        BetterCoreManagement.Log.LogDebug($"Adding 1 {researchCoreDefinition.coreType} Research Core Virtually");

                        __instance.myArray[i].GetInputInventory()
                            .RemoveResourcesFromSlot(coreLocation, out var num, 1, false);
                    }
                }
            }
        }
    }
}