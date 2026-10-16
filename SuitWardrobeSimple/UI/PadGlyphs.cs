using System.Runtime.CompilerServices;
using BepInEx.Bootstrap;
using LethalCompanyInputUtils.Glyphs;
using UnityEngine;

namespace SuitWardrobeSimple
{
    // Button icons from InputUtils, when it's installed. CorrectControllerIcon, if present, makes
    // InputUtils hand out the icons of the controller picked there.
    internal static class PadGlyphs
    {
        private static readonly bool HasInputUtils = Chainloader.PluginInfos.ContainsKey("com.rune580.LethalCompanyInputUtils");

        // Null without InputUtils, or when it has no icon for that button.
        internal static Sprite Get(string control)
        {
            return HasInputUtils ? FromInputUtils(control) : null;
        }

        // Kept in its own method so InputUtils is only loaded when it's there.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Sprite FromInputUtils(string control)
        {
            ControllerGlyph glyphs = ControllerGlyph.GetBestMatching();
            return glyphs != null ? glyphs["<Gamepad>/" + control] : null;
        }
    }
}
