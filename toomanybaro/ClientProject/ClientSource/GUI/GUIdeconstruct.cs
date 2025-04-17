using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Barotrauma;
using Microsoft.Xna.Framework;
using tooManyBaro.ClientSource;
using GUI = tooManyBaro.ClientSource.GUI;

namespace toomanybaro.ClientSource
{
    class GUIdeconstruct
    {

        /// <summary>
        /// Main function to draw the deconstruction tab. Will draw every item that produce our target when deconstructed.
        /// </summary>
        public static void drawDeconstruct(GUIFrame topFrame)
        {
            if (topFrame == null) return;
            var deconsFrame = new GUILayoutGroup(new RectTransform(new Vector2(1f, 1f), topFrame.RectTransform, anchor: Anchor.Center), childAnchor: Anchor.Center);
            var producewhenDecons = InventoryPatch.ProduceWhenDeconstruct;
            if (producewhenDecons != null && producewhenDecons.Count > 0)
            {
                var paddedProduceWhenDecons = new GUIListBox(new RectTransform(new Vector2(0.9f, 0.9f), deconsFrame.rectTransform), style: "ItemUI");
                var ProduceWhenDeconsFrame = new GUIListBox(new RectTransform(new Vector2(0.9f, 0.9f), paddedProduceWhenDecons.Content.rectTransform, anchor: Anchor.Center), style: null)
                {
                    Spacing = 20
                };
                foreach (var item in producewhenDecons)
                    if (item.AllowDeconstruct)
                    {
                        var il = new GUIListBox(new RectTransform(new Vector2(1f, 0.15f), ProduceWhenDeconsFrame.Content.rectTransform), style: null, isHorizontal: true);
                        var iIcon = new GUIListBox(new RectTransform(new Vector2(0.3f, 1f), il.Content.rectTransform), style: null, isHorizontal: true);
                        var boxItemIcon = new GUIFrame(new RectTransform(new Vector2(0.3f, 1f), iIcon.Content.rectTransform), style: "InnerGlowSmall");
                        var arrowFrame = new GUIFrame(new RectTransform(new Vector2(0.7f, 1f), iIcon.Content.RectTransform), style: null);
                        new GUIImage(new RectTransform(new Vector2(0.3f, 0.5f), arrowFrame.rectTransform, anchor: Anchor.Center), "GUIButtonHorizontalArrow", scaleToFit: false);
                        var boxItemIconCenter = new GUILayoutGroup(new RectTransform(new Vector2(0.9f, 0.9f), boxItemIcon.rectTransform, anchor: Anchor.Center), childAnchor: Anchor.Center);
                        var img = new GUIImage(new RectTransform(new Vector2(1f, 1f), boxItemIconCenter.rectTransform), item.InventoryIcon ?? item.sprite)
                        {
                            toolTip = RichString.Rich(item.GetTooltip(Character.controlled))
                        };
                        GUI.imageToItem.Add((img, item));
                        var pItems = new GUIListBox(new RectTransform(new Vector2(0.7f, 1f), il.Content.rectTransform), style: null, isHorizontal: true);
                        GUI.AddDeconstructToList(item.DeconstructItems.ToList(), pItems.Content.rectTransform);
                    }
            }
        }
    }
}
