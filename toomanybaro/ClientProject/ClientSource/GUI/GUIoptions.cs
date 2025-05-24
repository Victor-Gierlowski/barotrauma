using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Barotrauma;
using Barotrauma.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using tooManyBaro.ClientSource;
using static Barotrauma.CharacterInventory;

namespace toomanybaro.ClientSource
{
    class GUIoptions
    {
        public static void drawOptions(GUIFrame topFrame)
        {
            if (topFrame == null) return;
            var mainOptionsFrame = new GUIFrame(new RectTransform(new Vector2(1f, 1f), topFrame.rectTransform), style: null);
            var mainScrollableOption = new GUIListBox(new RectTransform(new Vector2(1f, 1f), mainOptionsFrame.rectTransform), style: null);
            var bigHorizontaleSplit = new GUIListBox(new RectTransform(new Vector2(1f, 1f), mainScrollableOption.Content.rectTransform), style: null, isHorizontal: true);
            var leftBHS = new GUIListBox(new RectTransform(new Vector2(0.5f, 1f), bigHorizontaleSplit.Content.rectTransform), style: null);
            var rightBHS = new GUIListBox(new RectTransform(new Vector2(0.5f, 1f), bigHorizontaleSplit.Content.rectTransform), style: null);


            // RefreshTimer
            var refreshTime_Frame = new GUIFrame(new RectTransform(new Vector2(1f, 0.1f), leftBHS.Content.rectTransform), style: null);
            var refreshTime_HList = new GUIListBox(new RectTransform(new Vector2(1f, 1f), refreshTime_Frame.rectTransform), style: null, isHorizontal: true)
            {
                ToolTip = TextManager.Get("TooManyBaro.options.tooltip.refreshtime")?.Value ?? "",
            };
            var optionName = TextManager.Get("TooManyBaro.options.name.refreshtime")?.Value ?? "Refresh Time";
            var refreshTime_Name = new GUITextBlock(new RectTransform(new Vector2(0.5f, 1f), refreshTime_HList.Content.rectTransform), optionName)
            {
                CanBeFocused = false,
                Wrap = true
            };
            var refreshTime_Input = new GUINumberInput(new RectTransform(new Vector2(0.3f, 1f), refreshTime_HList.Content.rectTransform), NumberType.Int)
            {
                OnValueChanged = (GUINumberInput input) =>
                {
                    int time = input.intValue;
                    if (time < 100 || time > 6 * 1e7)
                    {
                        time = Options.refresh_time;
                    }
                    else
                    {
                        Options.refresh_time = time;
                    }
                    if (tooManyBaro.ClientSource.GUI._Refresh_multiple_item != null)
                        tooManyBaro.ClientSource.GUI._Refresh_multiple_item.Interval = time;
                }
            };
            var v = Options.refresh_time;
            refreshTime_Input.TextBox.Text = $"{v}";

            // Reopen recipes
            var reopenRecipes_Frame = new GUIFrame(new RectTransform(new Vector2(1f, 0.1f), leftBHS.Content.rectTransform), style: null);
            var reopenRecipes_HList = new GUIListBox(new RectTransform(new Vector2(1f, 1f), reopenRecipes_Frame.rectTransform), style: null, isHorizontal: true)
            {
                ToolTip = TextManager.Get("TooManyBaro.options.tooltip.reopenrecipes")?.Value ?? "",
            };
            optionName = TextManager.Get("TooManyBaro.options.name.reopenRecipes")?.Value ?? "Reopen recipes upon close";
            var reopenRecipes_Name = new GUITextBlock(new RectTransform(new Vector2(0.5f, 1f), reopenRecipes_HList.Content.rectTransform), optionName)
            {
                CanBeFocused = false,
                Wrap = true
            };
            var reopenRecipes_Input = new GUITickBox(new RectTransform(new Vector2(0.3f, 0.3f), reopenRecipes_HList.Content.rectTransform), "")
            {
                isSelected = Options.reopen_recipes_after_close,
                OnSelected = (GUITickBox input) =>
                {
                    Options.reopen_recipes_after_close = input.isSelected;
                    return true;
                }
            };
            drawKeyBinds(rightBHS.Content.rectTransform);
        }

        static public bool inputBoxSelectedThisFrame = false;
        static public Action<KeyOrMouse>? currentSetter = null;
        static private readonly Dictionary<GUIButton, Func<LocalizedString>> inputButtonValueNameGetters = new Dictionary<GUIButton, Func<LocalizedString>>();
        static public GUICustomComponent? inputListener;
        static public void drawKeyBinds(RectTransform target)
        {
            GUIFrame keybindsFrame = new GUIFrame(new RectTransform(new Vector2(0.9f, 0.9f), target, anchor: Anchor.Center), style: null);

            GUIListBox keybindsListBox = new GUIListBox(new RectTransform(new Vector2(1f, 1f), keybindsFrame.RectTransform), style: null,isHorizontal:false);
            inputListener = new GUICustomComponent(new RectTransform(Vector2.Zero, target), onUpdate: (deltaTime, component) =>
            {
                if (currentSetter is null) { return; }

                if (PlayerInput.PrimaryMouseButtonClicked() && inputBoxSelectedThisFrame)
                {
                    inputBoxSelectedThisFrame = false;
                    return;
                }

                void clearSetter()
                {
                    currentSetter = null;
                    inputButtonValueNameGetters.Keys.ForEach(b => b.Selected = false);
                }

                void callSetter(KeyOrMouse v)
                {
                    currentSetter?.Invoke(v);
                    clearSetter();
                }

                var pressedKeys = PlayerInput.GetKeyboardState.GetPressedKeys();
                if (pressedKeys?.Any() ?? false)
                {
                    if (pressedKeys.Contains(Keys.Escape))
                    {
                        clearSetter();
                    }
                    else
                    {
                        callSetter(pressedKeys.First());
                    }
                }
            });
            GUIListBox openHUDHL = new GUIListBox(new RectTransform(new Vector2(1f, 0.1f), keybindsListBox.Content.RectTransform), style: null, isHorizontal: true)
            {
                ToolTip = TextManager.Get("TooManyBaro.options.tooltip.openHUD")?.Value ?? "",
            };
            new GUITextBlock(new RectTransform(new Vector2(0.2f, 0.8f), openHUDHL.Content.RectTransform),
                TextManager.Get("TooManyBaro.options.name.openHUD")?.Value ?? "Open HUD",
                font: GUIStyle.SmallFont)
            { CanBeFocused = false, Wrap = true };
            addInputToRow(openHUDHL,
                        "",
                        () => tooManyBaro.ClientSource.Options.userOptions?.openHUD.GetName() ?? "O",
                        v => {
                            if (tooManyBaro.ClientSource.Options.userOptions != null)
                                tooManyBaro.ClientSource.Options.userOptions.openHUD = v;
                            },
                        TextManager.Get("TooManyBaro.options.tooltip.openHUD") ?? ""
                        );
            
        }

        static private void addInputToRow(GUIListBox currRow, LocalizedString labelText, Func<LocalizedString> valueNameGetter, Action<KeyOrMouse> valueSetter, LocalizedString tooltip)
        {
            var inputFrame = new GUIFrame(new RectTransform((0.5f, 1.0f), currRow.Content.RectTransform),
                style: null);
            var label = new GUITextBlock(new RectTransform((0.6f, 1.0f), inputFrame.RectTransform), labelText,
                font: GUIStyle.SmallFont)
            { ForceUpperCase = ForceUpperCase.Yes };
            var inputBox = new GUIButton(
                new RectTransform((0.4f, 1.0f), inputFrame.RectTransform, Anchor.TopRight, Pivot.TopRight),
                valueNameGetter(), style: "GUITextBoxNoIcon")
            {
                OnClicked = (btn, obj) =>
                {
                    inputButtonValueNameGetters.Keys.ForEach(b =>
                    {
                        if (b != btn) { b.Selected = false; }
                    });
                    bool willBeSelected = !btn.Selected;
                    if (willBeSelected)
                    {
                        inputBoxSelectedThisFrame = true;
                        currentSetter = v =>
                        {
                            valueSetter(v);
                            btn.Text = valueNameGetter();
                        };
                    }

                    btn.Selected = willBeSelected;
                    return true;
                },
                ToolTip= tooltip
            };
            inputButtonValueNameGetters.Add(inputBox, valueNameGetter);
        }
    }
}
