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

        [Draw(DrawType.Slider, Min = 0, Max = 10, VisibleOn = "enableLandingDamage|true")] // TODO : Decide Min and Max values here
        public float minLandingThreshold; // TODO : Decide default value here
        [Draw(DrawType.Slider, Min = 0, Max = 10, VisibleOn = "enableLandingDamage|true")] // TODO : Decide Min and Max values here
        public float maxLandingThreshold; // TODO : Decide default value here

        [Draw(DrawType.Slider, Min = 0, Max = 100, VisibleOn = "enableLandingDamage|true")]
        public float landingPunctureProbability = 0.5f; // TODO : Decide default value here

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