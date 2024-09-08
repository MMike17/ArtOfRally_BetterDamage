using HarmonyLib;
using System;
using UnityModManagerNet;

using static UnityModManagerNet.UnityModManager;

namespace BetterDamage
{
    public class Main
    {
        public static bool enabled { get; private set; }

        public static ModEntry.ModLogger Logger;
        public static Settings settings;

        // Called by the mod manager
        static bool Load(ModEntry modEntry)
        {
            Logger = modEntry.Logger;
            settings = ModSettings.Load<Settings>(modEntry);

            // Harmony patching
            Harmony harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll();

            // hook in mod manager event
            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = (entry) => settings.Draw(entry);
            modEntry.OnSaveGUI = (entry) => settings.Save(entry);

            return true;
        }

        static bool OnToggle(ModEntry modEntry, bool state)
        {
            enabled = state;
            return true;
        }

        public static void Log(string message) => Logger.Log(message);

        public static void Error(string message) => Logger.Error(message);

        public static void Try(Action callback)
        {
            try
            {
                callback?.Invoke();
            }
            catch (Exception e)
            {
                Log(e.ToString());
            }
        }
    }
}
