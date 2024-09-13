using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

using static RepairsManagerUI;
using static UnityModManagerNet.UnityModManager;
using Random = UnityEngine.Random;

namespace BetterDamage
{
    // disable wear and tear
    [HarmonyPatch(typeof(PerformanceDamageManager), nameof(PerformanceDamageManager.DoWearAndTearStageDamage))]
    static class Patch
    {
        static bool Prefix() => !Main.enabled || !Main.settings.disableWearAndTear;
    }

    // TODO : Remove spammy logs
}