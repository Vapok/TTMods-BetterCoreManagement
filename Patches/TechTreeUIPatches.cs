using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Threading;
using HarmonyLib;
using JetBrains.Annotations;

namespace BetterCoreManagement.Patches
{
    public static class TechTreeUIPatches
    {
        [HarmonyPatch(typeof(TechTreeUI), nameof(TechTreeUI.RefreshCores))]
        public static class TechUIStartAddComponent
        {
            [UsedImplicitly]
            private static void Prefix(TechTreeUI __instance)
            {
                var coreTypesToDisplay = Traverse.Create(__instance).Field(nameof(__instance.coreTypesToDisplay)).GetValue() as List<ResearchCoreDefinition.CoreType>;

                if (coreTypesToDisplay != null)
                {
                    foreach (var coreCount in BetterCoreManagement.VirtualCoreCounts
                                 .Where(coreCount => coreCount.Value > 0)
                                 .Where(coreCount => !coreTypesToDisplay.Contains(coreCount.Key)))
                    {
                        coreTypesToDisplay.Add(coreCount.Key);
                    }
                }

                Traverse.Create(__instance).Field(nameof(__instance.coreTypesToDisplay)).SetValue(coreTypesToDisplay);
            }
            private static int UpdateTotalCores(int totalCores, int coreIndex)
            {
                return totalCores + BetterCoreManagement.VirtualCoreCounts[(ResearchCoreDefinition.CoreType)coreIndex];
            }
            
            [UsedImplicitly]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
                ILGenerator ilGenerator)
            {

                var instrs = instructions.ToList();

                var counter = 0;

                var patchedRefreshCoresMethod = false;

                CodeInstruction LogMessage(CodeInstruction instruction)
                {
                    BetterCoreManagement.Log.LogDebug(
                        $"IL_{counter}: Opcode: {instruction.opcode} Operand: {instruction.operand}");
                    return instruction;
                }

                CodeInstruction FindInstructionWithLabel(List<CodeInstruction> codeInstructions, int index, Label label)
                {
                    if (index >= codeInstructions.Count)
                        return null;

                    if (codeInstructions[index].labels.Contains(label))
                        return codeInstructions[index];

                    return FindInstructionWithLabel(codeInstructions, index + 1, label);
                }

                for (int i = 0; i < instrs.Count; ++i)
                {
                    if (i > 6 && instrs[i].opcode == OpCodes.Stloc_S && instrs[i - 1].opcode == OpCodes.Callvirt && 
                        instrs[i - 2].opcode == OpCodes.Ldloc_S &&
                        instrs[i - 3].opcode == OpCodes.Ldloc_0)
                    {
                        //Output current Store Location call
                        yield return LogMessage(instrs[i]);
                        counter++;

                        //Patch Load Total Cores to Stack
                        yield return LogMessage(new CodeInstruction(OpCodes.Ldloc_S,instrs[i].operand));
                        counter++;

                        //Patch Load Core Index to Stack
                        yield return LogMessage(new CodeInstruction(OpCodes.Ldloc_S,instrs[i - 2].operand));
                        counter++;

                        //Patch Call Method for Updating Total Cores.
                        yield return LogMessage(new CodeInstruction(OpCodes.Call,
                            AccessTools.DeclaredMethod(typeof(TechUIStartAddComponent), nameof(UpdateTotalCores))));
                        counter++;
                        
                        //Patch Store Total Cores from Stack
                        yield return LogMessage(new CodeInstruction(OpCodes.Stloc_S,instrs[i].operand));
                        counter++;

                        patchedRefreshCoresMethod = true;

                    }
                    else
                    {
                        yield return LogMessage(instrs[i]);
                        counter++;
                    }
                }
                
                if (!patchedRefreshCoresMethod)
                {
                    BetterCoreManagement.Log.LogError($"TechTreeUI.RefreshCores Transpiler Failed To Patch");
                    BetterCoreManagement.Log.LogError($"Please inform Mod Author.");
                    Thread.Sleep(5000);
                }
            }
        }
    }
}