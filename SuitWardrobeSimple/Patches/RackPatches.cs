using System.Reflection;
using BepInEx.Bootstrap;
using HarmonyLib;

namespace SuitWardrobeSimple.Patches
{
    [HarmonyPatch(typeof(StartOfRound), "PositionSuitsOnRack")]
    internal static class PositionSuitsOnRackPatch
    {
        [HarmonyPostfix]
        private static void Postfix() => RackFit.Refresh();
    }

    // TooManySuits turns the suit hitboxes back on with every page change.
    [HarmonyPatch]
    internal static class TooManySuitsPatches
    {
        private const string PageMethod = "TooManySuits.PaginationController:DisplayCurrentPage";

        private static bool Prepare() =>
            Chainloader.PluginInfos.ContainsKey("TooManySuits") && AccessTools.Method(PageMethod) != null;

        private static MethodBase TargetMethod() => AccessTools.Method(PageMethod);

        [HarmonyPostfix]
        private static void Postfix() => RackFit.Refresh();
    }
}
