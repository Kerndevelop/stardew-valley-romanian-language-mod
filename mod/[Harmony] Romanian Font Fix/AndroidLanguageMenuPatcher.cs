using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData;
using StardewValley.Menus;

namespace RomanianFontFix;

// Android's LanguageSelectionMenu hardcodes the 12 built-in languages and never reads
// Data/AdditionalLanguages, so mod languages have no button there. This adds one per entry.
// The Android-only members are reached through reflection because they don't exist in the
// desktop assemblies the mod is built against.
internal static class AndroidLanguageMenuPatcher
{
    private const int ScrollbarWidth = 24;

    // ModLanguage.ButtonTexture layout: normal state on top, highlighted state 39px below.
    private const int ButtonSpriteWidth = 174;
    private const int ButtonSpriteHeight = 40;
    private const int ButtonSpriteHoverY = 39;

    private static IMonitor Monitor = null!;

    private static FieldInfo LanguagesField = null!;
    private static FieldInfo ButtonWidthField = null!;
    private static FieldInfo ButtonHeightField = null!;
    private static FieldInfo HeightField = null!;
    private static FieldInfo MainBoxField = null!;
    private static FieldInfo ScrollbarField = null!;
    private static FieldInfo ScrollAreaField = null!;
    private static FieldInfo CurrentItemIndexField = null!;
    private static FieldInfo HavePanelScrolledField = null!;
    private static ConstructorInfo ScrollbarConstructor = null!;
    private static ConstructorInfo ScrollAreaConstructor = null!;
    private static MethodInfo SetUpForScrollBoxDrawingMethod = null!;
    private static MethodInfo FinishScrollBoxDrawingMethod = null!;

    private static readonly ConditionalWeakTable<ClickableComponent, ModLanguage> ModButtons = new();

    public static void Apply(Harmony harmony, IMonitor monitor)
    {
        Monitor = monitor;

        try
        {
            // Resolve everything first, so a renamed member leaves the menu unpatched rather
            // than half-patched.
            Type menuType = typeof(LanguageSelectionMenu);
            LanguagesField = RequireField(menuType, "languages");
            ButtonWidthField = RequireField(menuType, "buttonWidth");
            ButtonHeightField = RequireField(menuType, "buttonHeight");
            HeightField = RequireField(menuType, "height");
            MainBoxField = RequireField(menuType, "mainBox");
            ScrollbarField = RequireField(menuType, "newScrollbar");
            ScrollAreaField = RequireField(menuType, "scrollArea");
            CurrentItemIndexField = RequireField(menuType, "currentItemIndex");

            Type scrollbarType = ScrollbarField.FieldType;
            Type scrollAreaType = ScrollAreaField.FieldType;
            HavePanelScrolledField = RequireField(scrollAreaType, "havePanelScrolled");
            ScrollbarConstructor = AccessTools.Constructor(scrollbarType, new[] { typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(bool) })
                ?? throw new MissingMemberException(scrollbarType.FullName, ".ctor");
            ScrollAreaConstructor = AccessTools.Constructor(scrollAreaType, new[] { typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(Rectangle), scrollbarType })
                ?? throw new MissingMemberException(scrollAreaType.FullName, ".ctor");
            SetUpForScrollBoxDrawingMethod = RequireMethod(scrollAreaType, "setUpForScrollBoxDrawing", typeof(SpriteBatch), typeof(float));
            FinishScrollBoxDrawingMethod = RequireMethod(scrollAreaType, "finishScrollBoxDrawing", typeof(SpriteBatch), typeof(float));

            MethodInfo setupButtons = RequireMethod(menuType, "SetupButtons");
            MethodInfo releaseLeftClick = RequireMethod(menuType, "releaseLeftClick", typeof(int), typeof(int));
            MethodInfo receiveGamePadButton = RequireMethod(menuType, "receiveGamePadButton", typeof(Buttons));
            MethodInfo draw = RequireMethod(menuType, "draw", typeof(SpriteBatch));

            harmony.Patch(setupButtons, postfix: new HarmonyMethod(typeof(AndroidLanguageMenuPatcher), nameof(SetupButtons_Postfix)));
            harmony.Patch(releaseLeftClick, prefix: new HarmonyMethod(typeof(AndroidLanguageMenuPatcher), nameof(ReleaseLeftClick_Prefix)));
            harmony.Patch(receiveGamePadButton, prefix: new HarmonyMethod(typeof(AndroidLanguageMenuPatcher), nameof(ReceiveGamePadButton_Prefix)));
            harmony.Patch(
                draw,
                prefix: new HarmonyMethod(typeof(AndroidLanguageMenuPatcher), nameof(Draw_Prefix)),
                postfix: new HarmonyMethod(typeof(AndroidLanguageMenuPatcher), nameof(Draw_Postfix))
            );
        }
        catch (Exception ex)
        {
            monitor.Log($"Couldn't add mod languages to the Android language menu, so \"Română\" won't be selectable from it:\n{ex}", LogLevel.Warn);
        }
    }

    private static FieldInfo RequireField(Type type, string name) =>
        AccessTools.Field(type, name) ?? throw new MissingFieldException(type.FullName, name);

    private static MethodInfo RequireMethod(Type type, string name, params Type[] parameters) =>
        AccessTools.Method(type, name, parameters) ?? throw new MissingMethodException(type.FullName, name);

    // The game lays the menu out for 12 buttons; redo the same maths for the new total.
    private static void SetupButtons_Postfix(LanguageSelectionMenu __instance)
    {
        try
        {
            List<ModLanguage>? modLanguages = DataLoader.AdditionalLanguages(Game1.content);
            if (modLanguages is null || modLanguages.Count == 0)
                return;

            List<ClickableComponent> buttons = GetButtons(__instance);
            int buttonWidth = (int)ButtonWidthField.GetValue(__instance)!;
            int buttonHeight = (int)ButtonHeightField.GetValue(__instance)!;
            Rectangle oldBox = (Rectangle)MainBoxField.GetValue(__instance)!;

            int total = buttons.Count + modLanguages.Count;
            int viewportHeight = Game1.uiViewport.Height;
            int height = Math.Min(viewportHeight, total * buttonHeight + 32);
            bool needsScrolling = total * buttonHeight >= viewportHeight - 32;
            int boxWidth = buttonWidth + 32 + (needsScrolling ? ScrollbarWidth : 0);
            Rectangle box = new((Game1.uiViewport.Width - boxWidth) / 2, (viewportHeight - height) / 2, boxWidth, height);

            HeightField.SetValue(__instance, height);
            MainBoxField.SetValue(__instance, box);
            if (needsScrolling)
            {
                object scrollbar = ScrollbarConstructor.Invoke(new object[] { box.X + buttonWidth, box.Y + 16, ScrollbarWidth, height - 36, 0, 32, false });
                object scrollArea = ScrollAreaConstructor.Invoke(new object[]
                {
                    box.X, box.Y, buttonWidth, height,
                    total * buttonHeight - (height - 32),
                    new Rectangle(box.X + 16, box.Y + 16, buttonWidth, height - 32),
                    scrollbar,
                });
                ScrollbarField.SetValue(__instance, scrollbar);
                ScrollAreaField.SetValue(__instance, scrollArea);
            }

            foreach (ClickableComponent button in buttons)
            {
                button.bounds.X += box.X - oldBox.X;
                button.bounds.Y += box.Y - oldBox.Y;
            }

            foreach (ModLanguage language in modLanguages)
            {
                ClickableComponent button = new(new Rectangle(box.X + 16, box.Y + 16 + buttonHeight * buttons.Count, buttonWidth, buttonHeight), language.Id, null);
                ModButtons.AddOrUpdate(button, language);
                buttons.Add(button);
            }

            if (Game1.options.SnappyMenus)
                __instance.populateClickableComponentList();

            if (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.mod && LocalizedContentManager.CurrentModLanguage is { } current)
            {
                int index = buttons.FindIndex(button => ModButtons.TryGetValue(button, out ModLanguage? language) && language.Id == current.Id);
                if (index >= 0)
                {
                    CurrentItemIndexField.SetValue(__instance, index);
                    for (int i = 0; i < buttons.Count; i++)
                        buttons[i].label = i == index ? "hovered" : null;
                }
            }
        }
        catch (Exception ex)
        {
            Monitor.LogOnce($"Couldn't add mod languages to the Android language menu:\n{ex}", LogLevel.Warn);
        }
    }

    // The game treats a button name it doesn't know as English, so handle ours first.
    private static bool ReleaseLeftClick_Prefix(LanguageSelectionMenu __instance, int x, int y)
    {
        try
        {
            // Lifting a finger after dragging the list isn't a tap.
            object? scrollArea = ScrollAreaField.GetValue(__instance);
            if (scrollArea != null && (bool)HavePanelScrolledField.GetValue(scrollArea)!)
                return true;

            foreach (ClickableComponent button in GetButtons(__instance))
            {
                if (button.containsPoint(x, y) && ModButtons.TryGetValue(button, out ModLanguage? language))
                {
                    SelectLanguage(__instance, language);
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            Monitor.LogOnce($"Couldn't handle a tap in the Android language menu:\n{ex}", LogLevel.Warn);
        }

        return true;
    }

    private static bool ReceiveGamePadButton_Prefix(LanguageSelectionMenu __instance, Buttons b)
    {
        try
        {
            if (b != Buttons.A)
                return true;

            List<ClickableComponent> buttons = GetButtons(__instance);
            int index = (int)CurrentItemIndexField.GetValue(__instance)!;
            if (index >= 0 && index < buttons.Count && ModButtons.TryGetValue(buttons[index], out ModLanguage? language))
            {
                SelectLanguage(__instance, language);
                return false;
            }
        }
        catch (Exception ex)
        {
            Monitor.LogOnce($"Couldn't handle a gamepad press in the Android language menu:\n{ex}", LogLevel.Warn);
        }

        return true;
    }

    private static void SelectLanguage(LanguageSelectionMenu menu, ModLanguage language)
    {
        Game1.playSound("select");
        LocalizedContentManager.SetModLanguage(language);
        menu.exitThisMenu();
    }

    // The game would draw our buttons with the "English" sprite, so take them out of the list
    // while it draws and draw them from ButtonTexture afterwards.
    private static void Draw_Prefix(LanguageSelectionMenu __instance, out List<ClickableComponent>? __state)
    {
        __state = null;
        try
        {
            List<ClickableComponent> buttons = GetButtons(__instance);
            for (int i = buttons.Count - 1; i >= 0; i--)
            {
                if (!ModButtons.TryGetValue(buttons[i], out _))
                    continue;

                __state ??= new List<ClickableComponent>();
                __state.Insert(0, buttons[i]);
                buttons.RemoveAt(i);
            }
        }
        catch (Exception ex)
        {
            Monitor.LogOnce($"Couldn't prepare the Android language menu for drawing:\n{ex}", LogLevel.Warn);
        }
    }

    private static void Draw_Postfix(LanguageSelectionMenu __instance, SpriteBatch b, List<ClickableComponent>? __state)
    {
        if (__state is null)
            return;

        try
        {
            GetButtons(__instance).AddRange(__state);

            object? scrollArea = ScrollAreaField.GetValue(__instance);
            object[] scrollBoxArgs = { b, 1f };
            if (scrollArea != null)
                SetUpForScrollBoxDrawingMethod.Invoke(scrollArea, scrollBoxArgs);

            foreach (ClickableComponent button in __state)
            {
                if (!ModButtons.TryGetValue(button, out ModLanguage? language))
                    continue;

                Texture2D texture = Game1.temporaryContent.Load<Texture2D>(language.ButtonTexture);
                Rectangle source = new(0, button.label != null ? ButtonSpriteHoverY : 0, ButtonSpriteWidth, ButtonSpriteHeight);
                b.Draw(texture, button.bounds, source, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0f);
            }

            if (scrollArea != null)
                FinishScrollBoxDrawingMethod.Invoke(scrollArea, scrollBoxArgs);
        }
        catch (Exception ex)
        {
            Monitor.LogOnce($"Couldn't draw the mod languages in the Android language menu:\n{ex}", LogLevel.Warn);
        }
    }

    private static List<ClickableComponent> GetButtons(LanguageSelectionMenu menu) =>
        (List<ClickableComponent>)LanguagesField.GetValue(menu)!;
}
