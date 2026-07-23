using System;
using System.Collections.Generic;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Extensions;

namespace RomanianFontFix;

public class ModEntry : Mod
{
    // Source rects for the blank font_bold cells we drew the Romanian diacritics into.
    // See font-work/font_bold-work/CELL-MAPPING.md in the mod's source repo.
    // Every capital letter in font_bold already draws all the way to the bottom row of its
    // 8x16 cell (unlike lowercase, which leaves rows free below for descenders), so Ș's
    // comma-below mark has nowhere to sit without overlapping the S. Its rect is 3px taller,
    // reading into the row below (still blank in the atlas) so the comma can hang past the
    // S's normal bottom edge instead of sitting flush with it.
    private static readonly Dictionary<char, Rectangle> RomanianCharRects = new()
    {
        ['ă'] = CellRect(320),
        ['Ă'] = CellRect(321),
        ['ș'] = CellRect(322),
        ['Ș'] = CellRect(323, extraHeight: 3),
        ['ț'] = CellRect(324),
        ['Ț'] = CellRect(325),
    };

    private static Rectangle CellRect(int index, int extraHeight = 0) =>
        new(x: index % 16 * 8, y: index / 16 * 16, width: 8, height: 16 + extraHeight);

    public override void Entry(IModHelper helper)
    {
        Harmony harmony = new(this.ModManifest.UniqueID);
        harmony.Patch(
            original: AccessTools.Method(typeof(SpriteText), "getSourceRectForChar", new[] { typeof(char), typeof(bool) }),
            prefix: new HarmonyMethod(typeof(ModEntry), nameof(GetSourceRectForChar_Prefix))
        );
        harmony.Patch(
            original: AccessTools.Method(typeof(SpriteText), nameof(SpriteText.drawString)),
            prefix: new HarmonyMethod(typeof(ModEntry), nameof(DrawString_Prefix))
        );

        helper.Events.Content.AssetRequested += this.OnAssetRequested;
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        // Edit (not AssetReady) so this runs synchronously on the same thread that's loading the
        // asset — both patchers do GraphicsDevice work (RenderTarget2D/SpriteBatch), which needs
        // to happen on the main thread, not from a background content-loading callback.
        if (e.NameWithoutLocale.IsEquivalentTo("Fonts/SpriteFont1"))
            e.Edit(asset => SpriteFont1Patcher.Patch(this.Helper, this.Monitor, asset.GetData<SpriteFont>()));
        else if (e.NameWithoutLocale.IsEquivalentTo("Fonts/SmallFont"))
            e.Edit(asset => SmallFontPatcher.Patch(this.Helper, this.Monitor, asset.GetData<SpriteFont>()));
    }

    private static bool GetSourceRectForChar_Prefix(char c, bool junimoText, ref Rectangle __result)
    {
        if (junimoText || !RomanianCharRects.TryGetValue(c, out Rectangle rect))
            return true;

        __result = rect;
        return false;
    }

    // Romanian's "Încărcare în curs"/"Încărcare în curs..." (Game1.cs.3688/3689) starts with Î.
    // font_bold inks Î's circumflex at row 0 and its foot at row 15 of its 8x16 cell (measured
    // directly in font_bold.png) — i.e. it uses the *entire* cell — whereas a normal capital like
    // A only inks rows 4-11. SpriteText.drawString shifts every uppercase glyph up by a flat
    // 4 * FontPixelZoom screen px to align typical cap-height (assuming ~4px of blank margin
    // above the ink, which is what rows 0-3 give A but Î doesn't have), so Î ends up drawn
    // 4 * FontPixelZoom px higher than other capitals: with FontPixelZoom 3 (the default for
    // Latin/mod-language text), that's 12px — which lands its ink exactly on the scroll-background
    // banner's top edge (the touching we're fixing).
    //
    // Both banner call sites (TitleMenu's "Load"/"New" submenu transition, and Game1.DrawLoadScreen)
    // funnel into SpriteText.drawString with drawBGScroll:0, so patching it here covers both in one
    // place. drawString draws the banner and the text off the same (x, y) vector, so just passing
    // it a lower y (an earlier attempt, and a too-small +2 before that) shifts the banner down by
    // the same amount and doesn't gain any extra headroom — instead, draw the banner at the vanilla
    // position ourselves (copied from its drawBGScroll:0 case) and recurse into drawString with
    // drawBGScroll:-1 (no banner) and y offset by the 12px deficit, for the text only.
    private const int LoadingBannerTextDropPx = 12;

    private static bool DrawString_Prefix(
        SpriteBatch b, string s, int x, int y, int characterPosition, int width, int height,
        float alpha, float layerDepth, bool junimoText, int drawBGScroll,
        string placeHolderScrollWidthText, Color? color, SpriteText.ScrollTextAlignment scroll_text_alignment)
    {
        if (drawBGScroll != 0 || !IsRomanianLoadingBannerText(s))
            return true;

        int boxWidth = width != -1
            ? width
            : SpriteText.getWidthOfString(placeHolderScrollWidthText.Length > 0 ? placeHolderScrollWidthText : s);
        DrawLoadingBanner(b, x, y, boxWidth, alpha);
        SpriteText.drawString(b, s, x, y + LoadingBannerTextDropPx, characterPosition, width, height, alpha, layerDepth, junimoText, -1, placeHolderScrollWidthText, color, scroll_text_alignment);
        return false;
    }

    private static bool IsRomanianLoadingBannerText(string s)
    {
        if (LocalizedContentManager.CurrentLanguageCode != LocalizedContentManager.LanguageCode.mod
            || LocalizedContentManager.CurrentModLanguage?.LanguageCode != "ro")
            return false;

        // Game1.DrawLoadScreen appends 0-3 trailing dots that tick over time (e.g. "Încărcare în
        // curs..."), so this can't be an exact match — only TitleMenu's banner (Game1.cs.3689)
        // passes the string as-is.
        return s.StartsWith(Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3688"))
            || s.StartsWith(Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3689"));
    }

    private static void DrawLoadingBanner(SpriteBatch b, int x, int y, int width, float alpha)
    {
        float fontPixelZoom = SpriteText.FontPixelZoom;
        if (fontPixelZoom < 4f)
            y += (int)((4f - fontPixelZoom) * 4f);
        Vector2 vector = new(x, y);
        float boxLayerDepth = 0.88f - 0.001f;
        Color color = Color.White * alpha;
        b.Draw(Game1.mouseCursors, vector + new Vector2(-12f, -3f) * 4f, new Rectangle(325, 318, 12, 18), color, 0f, Vector2.Zero, 4f, SpriteEffects.None, boxLayerDepth);
        b.Draw(Game1.mouseCursors, vector + new Vector2(0f, -3f) * 4f, new Rectangle(337, 318, 1, 18), color, 0f, Vector2.Zero, new Vector2(width, 4f), SpriteEffects.None, boxLayerDepth);
        b.Draw(Game1.mouseCursors, vector + new Vector2(width, -12f), new Rectangle(338, 318, 12, 18), color, 0f, Vector2.Zero, 4f, SpriteEffects.None, boxLayerDepth);
    }
}
