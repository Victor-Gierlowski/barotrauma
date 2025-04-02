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
                };
                tooManyBaro.ClientSource.GUI.imageToItem.Add((img, item));
                count++;
            }}


    }
}
