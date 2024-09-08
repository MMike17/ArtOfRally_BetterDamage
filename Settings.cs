using UnityEngine;
using UnityModManagerNet;

using static UnityModManagerNet.UnityModManager;

namespace BetterDamage
{
    public class Settings : ModSettings, IDrawable
    {
        // [Draw(DrawType.)]

        [Header("Landing")]
        [Draw(DrawType.Toggle)]
        public bool enableLandingDamage;

        [Draw(DrawType.Slider, Min = 3, Max = 8, VisibleOn = "enableLandingDamage|true", Precision = 1)]
        public float minLandingThreshold = 5;
        [Draw(DrawType.Slider, Min = 5, Max = 20, VisibleOn = "enableLandingDamage|true", Precision = 1)]
        public float maxLandingThreshold = 13;

        [Draw(DrawType.Slider, Min = 0, Max = 5, VisibleOn = "enableLandingDamage|true", Precision = 1)]
        public float landingPunctureProbability = 0.5f;

        // TODO : Add puncture probability on crash
        // TODO : Add headlight damage probability on crash

        public override void Save(ModEntry modEntry)
        {
            InputValidation();
            Save(this, modEntry);
        }

        public void OnChange()
        {
            InputValidation();
        }

        void InputValidation()
        {
            maxLandingThreshold = Mathf.Max(maxLandingThreshold, minLandingThreshold);
        }
    }
}