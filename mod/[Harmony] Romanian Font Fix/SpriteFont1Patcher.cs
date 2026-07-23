using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;

namespace RomanianFontFix;

// Adds ă/Ă/ș/Ș/ț/Ț to Fonts/SpriteFont1 (Game1.dialogueFont). See DiacriticSpriteFontPatcher
// for the shared compositing/reflection mechanism (also used by SmallFontPatcher).
internal static class SpriteFont1Patcher
{
    // Filenames deliberately avoid differing only by case (e.g. not "a-breve.png" vs
    // "A-breve.png") — case-insensitive filesystems (Windows/Mac, which most players use)
    // would silently collide those into a single file.
    private static readonly NewGlyphSpec[] NewGlyphs =
    {
        new('ă', 'a', "assets/spritefont1-glyphs/breve-lower-a.png", MarkPosition.Above),
        new('Ă', 'A', "assets/spritefont1-glyphs/breve-upper-A.png", MarkPosition.Above),
        new('ș', 's', "assets/spritefont1-glyphs/comma-below-lower-s.png", MarkPosition.Below),
        new('Ș', 'S', "assets/spritefont1-glyphs/comma-below-upper-S.png", MarkPosition.Below),
        new('ț', 't', "assets/spritefont1-glyphs/comma-below-lower-t.png", MarkPosition.Below),
        new('Ț', 'T', "assets/spritefont1-glyphs/comma-below-upper-T.png", MarkPosition.Below),
    };

    public static void Patch(IModHelper helper, IMonitor monitor, SpriteFont font) =>
        DiacriticSpriteFontPatcher.Patch(helper, monitor, font, NewGlyphs, "Fonts/SpriteFont1");
}
