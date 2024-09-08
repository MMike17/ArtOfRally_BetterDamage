using System;
using System.Collections.Generic;
using System.Reflection;

using static RepairsManagerUI;

namespace BetterDamage
{
    public static class CarUtils
    {
        public static void DamagePart(PlayerCollider collider, float magnitudePercent, SystemToRepair targetPart)
        {
            float maxDamage = Main.GetField<float, PlayerCollider>(collider, "MaxDamage", BindingFlags.Instance);
            float multiplier = 0.5f;

            if (GameModeManager.GameMode == GameModeManager.GAME_MODES.CAREER)
                multiplier = SaveGame.GetInt("SETTINGS_CAREER_DAMAGE_LEVEL", 0) / 4f;
            else if (GameModeManager.GameMode == GameModeManager.GAME_MODES.CUSTOM)
                multiplier = SaveGame.GetInt("SETTINGS_CUSTOM_RALLY_DAMAGE_LEVEL", 0) / 4f;

            float totalDamage = magnitudePercent * maxDamage * multiplier;

            List<PerformanceDamage> partsList = Main.GetField<List<PerformanceDamage>, PerformanceDamageManager>(
                GameEntryPoint.EventManager.playerManager.performanceDamageManager,
                "DamageablePartsList",
                BindingFlags.Instance
            );
            PerformanceDamage selectedPart = partsList.Find(part => IsPartTarget(part, targetPart));

            if (selectedPart == null)
            {
                Main.Error("Couldn't find part for target : " + targetPart + ". Aborting.");
                return;
            }

            int index = partsList.IndexOf(selectedPart);

            Main.InvokeMethod(
                GameEntryPoint.EventManager.playerManager.performanceDamageManager,
                "DamageComponent",
                BindingFlags.Instance,
                new object[] { totalDamage, index }
            );
        }

        static bool IsPartTarget(PerformanceDamage part, SystemToRepair target)
        {
            switch (part)
            {
                case AerodynamicsPerformanceDamage body:
                    return target == SystemToRepair.CLEANCAR;

                case SteeringPerfomanceDamage suspension:
                    return target == SystemToRepair.SUSPENSION;

                case RadiatorPerformanceDamage radiator:
                    return target == SystemToRepair.RADIATOR;

                case EnginePerformanceDamage engine:
                    return target == SystemToRepair.ENGINE;

                case TurboPerformanceDamage turbo:
                    return target == SystemToRepair.TURBO;

                case TransmissionPerformanceDamage gearbox:
                    return target == SystemToRepair.GEARBOX;

                default:
                    throw new Exception("Didn't recognize the part");
            }
        }

        public static void PunctureTire(PlayerCollider collider, Wheel wheel)
        {
            wheel.DoTirePuncture();
            GameEntryPoint.EventManager.hudManager.ShowTirePunctureWarning();

            Main.GetField<SoundController, PlayerCollider>(
                collider,
                "soundController",
                BindingFlags.Instance
            ).PlayTirePunctureSound();
        }
    }
}
