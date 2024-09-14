using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace BetterDamage
{
    // disable wear and tear
    [HarmonyPatch(typeof(PerformanceDamageManager), nameof(PerformanceDamageManager.DoWearAndTearStageDamage))]
    static class Patch
    {
        static bool Prefix(PerformanceDamageManager __instance)
        {
            if (!Main.enabled || !Main.settings.disableWearAndTear)
                return true;

            if (Main.settings.wearAndTearBody)
            {
                PlayerCollider player = GameEntryPoint.EventManager.playerManager.PlayerObject.GetComponent<PlayerCollider>();
                float damageLevel = 0.5f;

                if (GameModeManager.GameMode == GameModeManager.GAME_MODES.CAREER)
                {
                    damageLevel = Main.InvokeMethod<PerformanceDamageManager, float>(
                        __instance,
                        "GetWearAndTearDamageLevel",
                        BindingFlags.Instance,
                        new object[] { SaveGame.GetInt("SETTINGS_CAREER_DAMAGE_LEVEL", 0) }
                    );
                }
                else if (GameModeManager.GameMode == GameModeManager.GAME_MODES.CUSTOM)
                {
                    damageLevel = Main.InvokeMethod<PerformanceDamageManager, float>(
                        __instance,
                        "GetWearAndTearDamageLevel",
                        BindingFlags.InvokeMethod,
                        new object[] { SaveGame.GetInt("SETTINGS_CUSTOM_RALLY_DAMAGE_LEVEL", 0) }
                    );
                }

                float levelLength = (Mathf.Clamp(GameModeManager.GetRallyDataCurrentGameMode().GetCurrentStage().Distance, 3f, 10f) + 10f) / 10;

                if (damageLevel != 0)
                    CarUtils.DamagePart(player, damageLevel * 0.0125f * levelLength, RepairsManagerUI.SystemToRepair.CLEANCAR);
            }

            return false;
        }
    }
}