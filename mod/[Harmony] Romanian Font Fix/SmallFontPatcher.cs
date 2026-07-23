using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;

namespace RomanianFontFix;

// Adds ă/Ă/ș/Ș/ț/Ț to Fonts/SmallFont (Game1.smallFont), used for item tooltip description
// text, the fishing catch/size popup, HUD toast messages, chat, and several menus. See
// DiacriticSpriteFontPatcher for the shared compositing/reflection mechanism, also used by
// SpriteFont1Patcher.
internal static class SmallFontPatcher
{
    private static readonly NewGlyphSpec[] NewGlyphs =
    {
        new('ă', 'a', "assets/smallfont-glyphs/breve-lower-a.png", MarkPosition.Above),
        new('Ă', 'A', "assets/smallfont-glyphs/breve-upper-A.png", MarkPosition.Above),
        new('ș', 's', "assets/smallfont-glyphs/comma-below-lower-s.png", MarkPosition.Below),
        new('Ș', 'S', "assets/smallfont-glyphs/comma-below-upper-S.png", MarkPosition.Below),
        new('ț', 't', "assets/smallfont-glyphs/comma-below-lower-t.png", MarkPosition.Below),
        new('Ț', 'T', "assets/smallfont-glyphs/comma-below-upper-T.png", MarkPosition.Below),
    };

    public static void Patch(IModHelper helper, IMonitor monitor, SpriteFont font) =>
        DiacriticSpriteFontPatcher.Patch(helper, monitor, font, NewGlyphs, "Fonts/SmallFont");
}
