using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Barotrauma;
using Microsoft.Xna.Framework;
using tooManyBaro.ClientSource;

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

        }
    }
}
