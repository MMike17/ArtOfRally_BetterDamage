using HarmonyLib;

namespace BetterDamage
{
    // Patch model
    // [HarmonyPatch(typeof(), nameof())]
    // [HarmonyPatch(typeof(), MethodType.)]
    // static class type_method_Patch
    // {
    // 	static void Prefix()
    // 	{
    // 		//
    // 	}

    // 	static void Postfix()
    // 	{
    // 		//
    // 	}
    // }

    // TARGETS :
    // - damage tires when you drift(detect when we drift/slip / chance of puncture)
    // - damage gearbox when you shift down and over rev / shift R when going forward / shift 1 when going back(complex to detect)
    // - damage engine whe you over rev(detect over rev)
    // - damage radiator when bump(detect the direction of bump)
    // - damage suspensions when bump(detect the direction of bump)
    // - damage turbo when overheat(detect when we overheat ? compare rev to forward speed / rev > ProjectForward(maxSpeed / 10))
}
