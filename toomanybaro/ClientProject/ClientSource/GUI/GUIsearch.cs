using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Barotrauma;
using Microsoft.Xna.Framework;

namespace tooManyBaro.ClientSource
{
    class GUIsearch
    {

        public static GUITextBox? searchInput;
        public static GUIListBox? searchResultList;

        /// <summary>
        /// Main function to draw the search tab
        /// </summary>
        /// <param name="masterFrame"></param>
        public static void drawSearch(GUIFrame masterFrame)
        {
            var masterVerticalSplit = new GUIListBox(new RectTransform(new Vector2(1f, 1f), masterFrame.RectTransform));
            searchInput = new GUITextBox(new RectTransform(new Vector2(1f, 0.1f), masterVerticalSplit.Content.rectTransform));
            searchInput.OnTextChanged += (GUITextBox textBox, string text) =>
            {
                InventoryPatch.searchItemWithString(text);
                update();
                return true;
            };
            searchResultList = new GUIListBox(new RectTransform(new Vector2(1f,0.9f),masterVerticalSplit.Content.rectTransform));
        }

        /// <summary>
        /// Upon new search, need to update all the item shown.
        /// </summary>
        public static void update()
        {
            tooManyBaro.ClientSource.GUI.clearImgToItem();
            if (searchResultList == null) return;
            searchResultList.ClearChildren();
            int count = 0;
            GUIListBox? targetList = null;
            foreach(var item in tooManyBaro.ClientSource.InventoryPatch.resultOfSearch)
            {
                if (count % 10 == 0 || targetList == null)
                    targetList = new GUIListBox(new RectTransform(new Vector2(1f, 0.1f), searchResultList.Content.RectTransform), isHorizontal:true);
                var itemFrame = new GUIFrame(new RectTransform(new Vector2(0.1f, 1f), targetList.Content.RectTransform), style: null);
                var img = new GUIImage(new RectTransform(new Vector2(1f, 1f), itemFrame.RectTransform), item.InventoryIcon ?? item.Sprite)
                {
                    ToolTip = RichString.Rich(item.GetTooltip(Character.Controlled)),
                    color = item.SpriteColor
                };
                tooManyBaro.ClientSource.GUI.imageToItem.Add((img, item));
                count++;
            }
        }


    }
}
