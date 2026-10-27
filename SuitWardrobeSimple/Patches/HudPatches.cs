using HarmonyLib;
using UnityEngine.InputSystem;

namespace SuitWardrobeSimple.Patches
{
    [HarmonyPatch(typeof(HUDManager), "PingScan_performed")]
    internal static class ScanPatch
    {
        // Other mods' prefixes still run after a skip (ShipLootCruiser shows its counter), so the press is cleared for them too.
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static bool Prefix(ref InputAction.CallbackContext context)
        {
            if (!WardrobeMenu.IsOpen)
                return true;

            context = default;
            return false;
        }
    }
}
