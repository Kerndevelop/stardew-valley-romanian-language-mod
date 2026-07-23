using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;

namespace RomanianFontFix;

// Shared by SpriteFont1Patcher and SmallFontPatcher: both fonts are real compiled XNA
// SpriteFonts (unlike font_bold, a plain texture SpriteText reads via a hardcoded switch
// statement, fixed via a Harmony prefix instead). A SpriteFont's glyph list is baked into
// private fields when the XNB loads, and there's no public API to add a glyph afterwards.
// The approach here (matching the technique used by the FontSmasher mod): build a brand new
// SpriteFont via its public constructor with the extra glyphs included, then copy its private
// fields (_glyphs/_regions/_texture/_defaultGlyphIndex/Characters) onto the live instance, so
// every existing reference to the original object sees the change.
internal enum MarkPosition
{
    Above, // breve: canvas only grew upward from the base letter, bottom edge unchanged
    Below, // comma: canvas only grew downward from the base letter, top edge unchanged
}

internal sealed record NewGlyphSpec(char Character, char BaseCharacter, string AssetName, MarkPosition Mark);

internal static class DiacriticSpriteFontPatcher
{
    private static readonly FieldInfo GlyphsField = typeof(SpriteFont).GetField("_glyphs", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo RegionsField = typeof(SpriteFont).GetField("_regions", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo TextureField = typeof(SpriteFont).GetField("_texture", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo DefaultGlyphIndexField = typeof(SpriteFont).GetField("_defaultGlyphIndex", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo CharactersBackingField = typeof(SpriteFont).GetField("<Characters>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)!;

    public static void Patch(IModHelper helper, IMonitor monitor, SpriteFont font, NewGlyphSpec[] newGlyphs, string assetLabel)
    {
        Dictionary<char, SpriteFont.Glyph> glyphs = font.GetGlyphs();
        if (glyphs.ContainsKey(newGlyphs[0].Character))
            return;

        Texture2D oldTexture = (Texture2D)TextureField.GetValue(font)!;

        Dictionary<char, Texture2D> glyphTextures = newGlyphs.ToDictionary(
            g => g.Character,
            g => helper.ModContent.Load<Texture2D>(g.AssetName));

        int newRowHeight = glyphTextures.Values.Max(t => t.Height);
        int newWidth = oldTexture.Width;
        int newHeight = oldTexture.Height + newRowHeight;
        if (glyphTextures.Values.Sum(t => t.Width) > newWidth)
            throw new InvalidOperationException($"Romanian {assetLabel} glyphs don't fit in a single new texture row.");

        var placements = new Dictionary<char, Rectangle>();
        int x = 0;
        foreach (NewGlyphSpec spec in newGlyphs)
        {
            Texture2D tex = glyphTextures[spec.Character];
            placements[spec.Character] = new Rectangle(x, oldTexture.Height, tex.Width, tex.Height);
            x += tex.Width;
        }

        // The vanilla texture isn't a plain 32bpp Color texture (Texture2D.GetData<Color> throws
        // on it directly), so rather than guess at its real pixel format, let the GPU do the format
        // conversion: draw the old texture plus the new glyphs onto a Color-format render target,
        // then read that back.
        GraphicsDevice device = oldTexture.GraphicsDevice;
        RenderTargetBinding[] previousTargets = device.GetRenderTargets();
        Texture2D newTexture;
        using (RenderTarget2D renderTarget = new(device, newWidth, newHeight, false, SurfaceFormat.Color, DepthFormat.None))
        {
            device.SetRenderTarget(renderTarget);
            device.Clear(Color.Transparent);
            using (SpriteBatch spriteBatch = new(device))
            {
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
                spriteBatch.Draw(oldTexture, Vector2.Zero, Color.White);
                foreach (NewGlyphSpec spec in newGlyphs)
                    spriteBatch.Draw(glyphTextures[spec.Character], new Vector2(placements[spec.Character].X, placements[spec.Character].Y), Color.White);
                spriteBatch.End();
            }
            device.SetRenderTargets(previousTargets);

            Color[] newPixels = new Color[newWidth * newHeight];
            renderTarget.GetData(newPixels);
            newTexture = new Texture2D(device, newWidth, newHeight);
            newTexture.SetData(newPixels);
        }

        foreach (NewGlyphSpec spec in newGlyphs)
        {
            SpriteFont.Glyph baseGlyph = glyphs[spec.BaseCharacter];
            Rectangle bounds = placements[spec.Character];
            int cropY = spec.Mark == MarkPosition.Below
                ? baseGlyph.Cropping.Y
                : baseGlyph.Cropping.Y - (bounds.Height - baseGlyph.BoundsInTexture.Height);

            glyphs[spec.Character] = new SpriteFont.Glyph
            {
                Character = spec.Character,
                BoundsInTexture = bounds,
                Cropping = new Rectangle(0, cropY, bounds.Width, baseGlyph.Cropping.Height),
                LeftSideBearing = baseGlyph.LeftSideBearing,
                RightSideBearing = baseGlyph.RightSideBearing,
                Width = baseGlyph.Width,
                WidthIncludingBearings = baseGlyph.WidthIncludingBearings,
            };
        }

        List<SpriteFont.Glyph> allGlyphs = glyphs.Values.OrderBy(g => g.Character).ToList();
        List<char> characters = allGlyphs.Select(g => g.Character).ToList();
        List<Rectangle> boundsInTexture = allGlyphs.Select(g => g.BoundsInTexture).ToList();
        List<Rectangle> cropping = allGlyphs.Select(g => g.Cropping).ToList();
        List<Vector3> kerning = allGlyphs.Select(g => new Vector3(g.LeftSideBearing, g.Width, g.RightSideBearing)).ToList();

        SpriteFont newFont = new(newTexture, boundsInTexture, cropping, characters, font.LineSpacing, font.Spacing, kerning, font.DefaultCharacter);

        GlyphsField.SetValue(font, GlyphsField.GetValue(newFont));
        RegionsField.SetValue(font, RegionsField.GetValue(newFont));
        TextureField.SetValue(font, TextureField.GetValue(newFont));
        DefaultGlyphIndexField.SetValue(font, DefaultGlyphIndexField.GetValue(newFont));
        CharactersBackingField.SetValue(font, CharactersBackingField.GetValue(newFont));

        monitor.Log($"Added ă/Ă/ș/Ș/ț/Ț to {assetLabel}.", LogLevel.Trace);
    }
}
