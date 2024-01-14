using HarmonyLib;
using JetBrains.Annotations;

namespace BetterCoreManagement.Patches
{
    public static class SaveStatePatches
    {
        [HarmonyPatch(typeof(SaveState), nameof(SaveState.LoadFileData), typeof(SaveState.SaveMetadata), typeof(string))]
        public static class SaveStateLoadFileData
        {
            public static void Postfix(SaveState __instance, SaveState.SaveMetadata saveMetadata)
            {
                BetterCoreManagement.Instance.LoadVirtualCounts(saveMetadata.worldName);
            }
        }

        [HarmonyPatch(typeof(SaveState), nameof(SaveState.SaveToFile))]
        public static class SaveStateSaveToFile
        {
            [UsedImplicitly]
            public static void Postfix(SaveState __instance)
            {
                if (__instance?.metadata?.worldName == null)
                    return;
                
                BetterCoreManagement.Instance.SaveVirtualCounts(__instance.metadata.worldName);
            }
        }
    }
}