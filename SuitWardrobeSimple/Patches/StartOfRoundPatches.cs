using HarmonyLib;

namespace SuitWardrobeSimple.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal static class StartOfRoundPatches
    {
        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        private static void RememberVanillaSuits(StartOfRound __instance)
        {
            SuitSources.RememberVanilla(__instance);
        }

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        private static void AttachToRack(StartOfRound __instance)
        {
            RackTrigger.Attach(__instance);
            WardrobeMenu.Prepare();
        }

        [HarmonyPatch(nameof(StartOfRound.OnDestroy))]
        [HarmonyPrefix]
        private static void CloseWardrobe()
        {
            WardrobeMenu.Dispose();
        }
    }
}
