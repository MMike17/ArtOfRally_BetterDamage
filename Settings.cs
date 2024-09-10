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
        public bool showMarkers;

        [Header("Landing")]
        [Draw(DrawType.Toggle)]
        public bool enableLandingDamage;

        [Draw(DrawType.Slider, Min = 3, Max = 8, VisibleOn = "enableLandingDamage|true", Precision = 1)]
        public float minLandingThreshold = 5;
        [Draw(DrawType.Slider, Min = 5, Max = 20, VisibleOn = "enableLandingDamage|true", Precision = 1)]
        public float maxLandingThreshold = 13;
        [Draw(DrawType.Slider, Min = 0, Max = 2, VisibleOn = "enableLandingDamage|true")]
        public float landingDamageMultiplier = 1;

        [Draw(DrawType.Slider, Min = 0, Max = 100, VisibleOn = "enableLandingDamage|true")]
        public float landingPunctureThreshold = 90;
        [Draw(DrawType.Slider, Min = 0, Max = 10, VisibleOn = "enableLandingDamage|true", Precision = 1)]
        public float landingPunctureProbability = 0.5f;

        [Header("Crash")]
        [Draw(DrawType.Slider, Min = 0, Max = 100)]
        public float crashPunctureThreshold = 90;
        [Draw(DrawType.Slider, Min = 0, Max = 10, Precision = 1)]
        public float crashPunctureProbability = 0.5f;
        [Draw(DrawType.Slider, Min = 0, Max = 50, Precision = 1)]
        public float crashHeadlightProbability = 8;

        public override void Save(ModEntry modEntry)
        {
            InputValidation();
            Save(this, modEntry);
        }

        public void OnChange()
        {
            InputValidation();
            Main.SetMarkers(showMarkers);
        }

        void InputValidation()
        {
            maxLandingThreshold = Mathf.Max(maxLandingThreshold, minLandingThreshold);
        }
    }
}