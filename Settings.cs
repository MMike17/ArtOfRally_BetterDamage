using UnityEngine;
using UnityModManagerNet;

using static UnityModManagerNet.UnityModManager;

namespace BetterDamage
{
    public class Settings : ModSettings, IDrawable
    {
        // [Draw(DrawType.)]

        [Header("Debug")]
        [Draw(DrawType.Toggle)]
        public bool showMarkers = false;

        [Header("Landing")]
        [Draw(DrawType.Toggle)]
        public bool enableLandingDamage = true;
        [Space]
        [Draw(DrawType.Slider, Min = 3, Max = 8, VisibleOn = "enableLandingDamage|true", Precision = 1)]
        public float minLandingThreshold = 5;
        [Draw(DrawType.Slider, Min = 5, Max = 20, VisibleOn = "enableLandingDamage|true", Precision = 1)]
        public float maxLandingThreshold = 13;
        [Draw(DrawType.Slider, Min = 0, Max = 2, VisibleOn = "enableLandingDamage|true", Precision = 0)]
        public float landingDamageMultiplier = 1;
        [Space]
        [Draw(DrawType.Slider, Min = 0, Max = 100, VisibleOn = "enableLandingDamage|true", Precision = 0)]
        public float landingPunctureThreshold = 90;
        [Draw(DrawType.Slider, Min = 0, Max = 5, VisibleOn = "enableLandingDamage|true", Precision = 1)]
        public float landingPunctureProbability = 0.5f;

        [Header("Crash")]
        [Draw(DrawType.Slider, Min = 0, Max = 100, Precision = 0)]
        public float crashPunctureThreshold = 90;
        [Draw(DrawType.Slider, Min = 0, Max = 5, Precision = 1)]
        public float crashPunctureProbability = 0.5f;
        [Draw(DrawType.Slider, Min = 0, Max = 50, Precision = 1)]
        public float crashHeadlightProbability = 8;

        [Header("Drift")]
        [Draw(DrawType.Toggle)]
        public bool enableDriftDamage = true;
        [Draw(DrawType.Slider, Min = 0, Max = 10, VisibleOn = "enableDriftDamage|true", Precision = 1)]
        public float driftPunctureProbability = 1f;

        [Header("Overheat")]
        [Draw(DrawType.Toggle)]
        public bool enableOverheatDamage = true;
        [Draw(DrawType.Slider, Min = 75, Max = 85, VisibleOn = "enableOverheatDamage|true", Precision = 0)]
        public float overheatRPMThresholdPercent = 85;
        [Draw(DrawType.Slider, Min = 50, Max = 80, VisibleOn = "enableOverheatDamage|true", Precision = 0)]
        public float overheatRPMBalancePercent = 75;
        [Draw(DrawType.Slider, Min = 0.1f, Max = 1.5f, VisibleOn = "enableOverheatDamage|true", Precision = 1)]
        public float overheatCooldownSpeedMult = 0.7f;

        public override void Save(ModEntry modEntry)
        {
            InputValidation();
            Save(this, modEntry);
        }

        public void OnChange()
        {
            InputValidation();
            Main.SetMarkers(showMarkers);
            OverheatManager.Refresh();
        }

        void InputValidation()
        {
            maxLandingThreshold = Mathf.Max(maxLandingThreshold, minLandingThreshold);
            overheatRPMBalancePercent = Mathf.Min(overheatRPMThresholdPercent, overheatRPMBalancePercent);
        }
    }
}