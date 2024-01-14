using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BetterCoreManagement
{
    [BepInPlugin(MyGUID, PluginName, VersionString)]
    [BepInIncompatibility("com.equinox.VirtualCores")]
    public class BetterCoreManagement : BaseUnityPlugin
    {
        private const string MyGUID = "com.vapok.BetterCoreManagement";
        private const string PluginName = "BetterCoreManagement";
        private const string VersionString = "1.0.3";

        public static BetterCoreManagement Instance => _instance;
        public static ManualLogSource Log = new ManualLogSource(PluginName);
        public static Dictionary<ResearchCoreDefinition.CoreType, int> VirtualCoreCounts => _virtualCoreCounts;
        
        private static BetterCoreManagement _instance;
        private static Harmony _harmony = new Harmony(MyGUID);
        private static Dictionary<ResearchCoreDefinition.CoreType, int> _virtualCoreCounts;
        private string _saveFolder = Path.Combine(Application.persistentDataPath,"BetterCoreManagement");
        
        private void Awake()
        {
            gameObject.hideFlags = HideFlags.HideAndDontSave;
            _instance = this;
            Logger.LogInfo($"{PluginName} [{VersionString}] is loading...");
            Log = Logger;
            
            Directory.CreateDirectory(_saveFolder);

            _virtualCoreCounts = new Dictionary<ResearchCoreDefinition.CoreType, int>();

            foreach (var coreType in Enum.GetNames(typeof(ResearchCoreDefinition.CoreType)))
            {
                if (Enum.TryParse<ResearchCoreDefinition.CoreType>(coreType, out var core))
                {
                    if (!_virtualCoreCounts.ContainsKey(core))
                    {
                        _virtualCoreCounts.Add(core,0);
                    }
                }
            }
            
            _harmony.PatchAll();
        }

        public void SaveVirtualCounts(string worldName)
        {
            if (_virtualCoreCounts == null)
                return;
            
            var fileName = Path.Combine(_saveFolder, $"{worldName}.bin");
            var bf = new BinaryFormatter();
            var fs = File.Create(fileName);
            
            Log.LogDebug($"Saving Virtual Core Data to {worldName}.bin");
            
            bf.Serialize(fs,_virtualCoreCounts);
            fs.Close();
        }

        public void LoadVirtualCounts(string worldName)
        {
            var fileName = Path.Combine(_saveFolder, $"{worldName}.bin");
            
            if (!File.Exists(fileName))
                return;
            
            Log.LogDebug($"Loading Virtual Core Data from {worldName}.bin");
            var bf = new BinaryFormatter();
            var fs = File.Open(fileName,FileMode.Open);
            var tempCounts = bf.Deserialize(fs) as Dictionary<ResearchCoreDefinition.CoreType, int>;
            fs.Close();

            if (tempCounts == null)
                return;
            Log.LogDebug($"Loading Core Counts From File:");
            foreach (var keyVal in tempCounts)
            {
                Log.LogDebug($"{keyVal.Key} Core: {keyVal.Value}");
                _virtualCoreCounts[keyVal.Key] = keyVal.Value;
            }
        }
    }
}