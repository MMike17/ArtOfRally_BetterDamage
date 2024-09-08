using UnityModManagerNet;
using static UnityModManagerNet.UnityModManager;

namespace BetterDamage
{
    public class Settings : ModSettings, IDrawable
    {
        // [Draw(DrawType.)]

        public override void Save(ModEntry modEntry) => Save(this, modEntry);

        public void OnChange()
        {
            //
        }
    }
}