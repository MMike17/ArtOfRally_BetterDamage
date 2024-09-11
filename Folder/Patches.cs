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
    // /!\ TODO : DISABLE DAMAGES WHEN IN REPLAY MODE /!\

    // TODO : Disable the "wear and tear" at the end of a stage (except for body ?)
    // TODO : Add body damage by default

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

    // TODO : damage gearbox when you shift down and over rev
    // TODO : damage gearbox when you hit the back of the car
    // TODO : damage turbo when overheat (detect when we overheat ? compare rev to forward speed / rev > ProjectForward(maxSpeed / 10))
}