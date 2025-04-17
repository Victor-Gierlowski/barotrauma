using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Barotrauma;
using Microsoft.Xna.Framework;
using tooManyBaro.ClientSource;

namespace tooManyBaro.ClientSource
{
    class GUIinfos
    {
        public static bool draw_info_buy = true;

        public static GUIFrame? infosPadded;
        public static GUILayoutGroup? infosFrame;
        public static GUIListBox? infosFrameVerticalSplit;

        private static List<(String, String)> allTexts = new();
        private static int bigger = 0;

        /// <summary>
        /// Main function to draw the infos tab about our current item target.
        /// </summary>
        /// <param name="topFrame"></param>
        public static void drawInfos(GUIFrame topFrame)
        {
            if (topFrame == null) return;
            if (InventoryPatch.LastOver == null) return;
            if (infosPadded != null)
            {
                infosPadded.ClearChildren();
            }
            //drawInfoAmmo(topFrame.rectTransform);

            infosPadded = new GUIFrame(new RectTransform(new Vector2(0.96f, 0.96f), topFrame.rectTransform, Anchor.Center), style: "itemUI");
            infosFrame = new GUILayoutGroup(new RectTransform(new Vector2(0.9f, 0.9f), infosPadded.rectTransform, anchor: Anchor.Center));
            infosFrameVerticalSplit = new GUIListBox(new RectTransform(new Vector2(1f, 1f), infosFrame.rectTransform), style: null)
            {
                Spacing = 40,
            };
            // TopSide Vertical split for tooltip and icon
            var infosFrameHorizontalSplitList = new GUIListBox(new RectTransform(new Vector2(1f, 0.4f), infosFrameVerticalSplit.Content.rectTransform), isHorizontal: true, style: null)
            {
                Spacing = 30
            };
            // LEFT SIDE FOR THE ICON
            var leftSideFrame = new GUILayoutGroup(new RectTransform(new Vector2(0.15f, 1f), infosFrameHorizontalSplitList.Content.rectTransform));
            var icon = InventoryPatch.LastOver.InventoryIcon ?? InventoryPatch.LastOver.Sprite;
            if (icon != null)
                new GUIImage(new RectTransform(new Vector2(1f, 1f), leftSideFrame.rectTransform, anchor: Anchor.Center), icon)
                {
                    color = InventoryPatch.LastOver.SpriteColor
                };
            new GUIFrame(new RectTransform(new Vector2(0.01f, 0.6f), infosFrameHorizontalSplitList.Content.RectTransform), style: "VerticalLine");
            // Right side for the tooltip
            var rightSideFrame = new GUILayoutGroup(new RectTransform(new Vector2(0.6f, 1f), infosFrameHorizontalSplitList.Content.rectTransform));
            new GUITextBlock(new RectTransform(new Vector2(1f, 1f), rightSideFrame.rectTransform), RichString.Rich(InventoryPatch.LastOver.GetTooltip(Character.controlled)))
            {
                Wrap = true,
            };
            // Bottom side, diverse infos.
            var priceFrameNotScrollable = new GUIFrame(new RectTransform(new Vector2(1f, 0.6f), infosFrameVerticalSplit.Content.rectTransform), style: null);
            var priceFrame = new GUIListBox(new RectTransform(new Vector2(1f, 1f), priceFrameNotScrollable.rectTransform), style: null);
            var priceFrameHorizontaleSplit = new GUIListBox(new RectTransform(new Vector2(1f, 1f), priceFrame.Content.rectTransform), isHorizontal: true, style: null);
            var priceFrameBooleanVerticalSplit = new GUIListBox(new RectTransform(new Vector2(0.3f, 1f), priceFrameHorizontaleSplit.Content.rectTransform), style: null)
            {
                Spacing = 20
            };
            var priceFramePriceValuesVerticalSplit = new GUIListBox(new RectTransform(new Vector2(0.3f, 1f), priceFrameHorizontaleSplit.Content.rectTransform), style: null)
            {
                Spacing = 20
            };
            var pricePerMerchantFramePriceValuesVerticalSplit = new GUIListBox(new RectTransform(new Vector2(0.4f, 1f), priceFrameHorizontaleSplit.Content.rectTransform), style: null)
            {
                Spacing = 20
            };
            string reputationNeeded = "";
            FactionPrefab? foundFaction = null;
            if (FactionPrefab.Prefabs.Any() && InventoryPatch.LastOver.DefaultPrice != null && InventoryPatch.LastOver.DefaultPrice.RequiredFaction != null)
                foundFaction = FactionPrefab.Prefabs.Find(f => f.Identifier == InventoryPatch.LastOver.DefaultPrice.RequiredFaction);
            string faction = "";
            if (foundFaction != null)
                faction = foundFaction.Name.ToString();
            float reput = -1;
            //string marchant = "";
            var e = InventoryPatch.LastOver.DefaultPrice;
            if (e == null) return;
            if (e.MinReputation.Any())
            {
                foreach (var b in e.MinReputation)
                {

                    reput = b.Value;
                    DebugConsole.NewMessage($"va {b.Value} - kb {b.Key.ToString()}");
                }
            }

            foreach (var a in InventoryPatch.LastOver.StorePrices)
            {
                if (a.Value.MinReputation.Any())
                {
                    foreach (var b in a.Value.MinReputation)
                    {

                        reput = b.Value;
                        var f = FactionPrefab.Prefabs.Find(pf => pf.Identifier == b.Key);
                        if (f != null)
                        {
                            faction = f.Name.Value;
                        }
                        DebugConsole.NewMessage($"v {b.Value} - k {b.Key.ToString()}");
                    }
                }
            }
            if (reput != -1)
                reputationNeeded += $"[‖color:{GUIStyle.ColorReputationVeryHigh.Value}‖{reput}‖color:end‖]";

            var needUnlock = false;
            if (InventoryPatch.LastOver.DefaultPrice != null)
                needUnlock = InventoryPatch.LastOver.DefaultPrice.RequiresUnlock;
            newText(("Can be Bought", GUI.addBoolRichString(InventoryPatch.LastOver.CanBeBought).ToString()));
            newText(("Can be Sold", GUI.addBoolRichString(InventoryPatch.LastOver.CanBeSold).ToString()));
            newText(("Need unlock", GUI.addBoolRichString(needUnlock).ToString()));
            newText(("Could you buy", GUI.addBoolRichString(InventoryPatch.LastOver.CanCharacterBuy() && InventoryPatch.LastOver.CanBeBought).ToString()));
            DrawAllText(priceFrameBooleanVerticalSplit.Content.rectTransform);
            newText(("Price", $"{InventoryPatch.LastOver.defaultPrice.Price}"));
            newText(("Buying Factor", $"{InventoryPatch.LastOver.defaultPrice.BuyingPriceMultiplier}x"));
            newText(("Min Price", $"{InventoryPatch.LastOver.GetMinPrice()}"));
            newText(("faction", $"{faction}"));
            newText(("Reputation", $"{reputationNeeded}"));
            DrawAllText(priceFramePriceValuesVerticalSplit.Content.rectTransform);
            DrawPricesWithinInfo(pricePerMerchantFramePriceValuesVerticalSplit);

        }

        /// <summary>
        /// Create a new text and count it's size. should be use to align item with monospace font
        /// </summary>
        /// <param name="t"></param>
        private static void newText((string, string) t)
        {
            if (t.Item1.Length > bigger) bigger = t.Item1.Length;
            allTexts.Add(t);
        }

        /// <summary>generate a list of text block from the allTexts list. Can be supplied with custom expression to generate the text to fill from the list.</summary>
        ///<example>drawAllText(priceFrameBooleanVerticalSplit.Content.rectTransform,
        ///id => { return string.Concat(id, string.Concat(Enumerable.Repeat(" ", (bigger - id.Length)))); },
        ///    (id, text) => { return $"{id}: ‖color:{Color.Gold}‖{text}‖color:end‖"; }
        ///    );
        ///</ example>
        private static void DrawAllText(RectTransform target, Func<String, String>? IdExpression, Func<String, String, String>? TextExpression)
        {
            if (IdExpression == null) IdExpression = id => { return string.Concat(id, string.Concat(Enumerable.Repeat(" ", (bigger - id.Length)))); };
            if (TextExpression == null) TextExpression = (id, text) => { return $"{id}: ‖color:{Color.Gold}‖{text}‖color:end‖"; };

            foreach (var tuple in allTexts)
            {
                String id = IdExpression(tuple.Item1);
                RichString text = TextExpression(tuple.Item1, tuple.Item2);
                new GUITextBlock(new RectTransform(new Vector2(1f, 0.1f), target), RichString.Rich(text))
                {
                    CanBeFocused = false,
                    Font = GUIStyle.MonospacedFont,
                    TextSize = new Vector2(3f, 3f)
                };
            }
            bigger = 0;
            allTexts.Clear();
        }

        private static void DrawAllText(RectTransform target)
        {
            DrawAllText(target, null, null);
        }

        /// <summary>
        /// Calculate and draw prices of the item base on either the buy or sell values.
        /// </summary>
        /// <param name="targetList"></param>
        private static void DrawPricesWithinInfo(GUIListBox targetList)
        {
            targetList.ClearChildren();
            if (InventoryPatch.LastOver == null) return;
            var buttonSwapPriceBuyOrSellf = new GUIButton(new RectTransform(new Vector2(1f, 0.1f), targetList.Content.rectTransform)
            {
            }, (draw_info_buy) ? "Sell" : "Buy")
            {
                OnClicked = (GUIButton button, object obj) =>
                {
                    if (draw_info_buy)
                    {
                        draw_info_buy = false;
                        button.textBlock.text = "Buy";
                    }
                    else
                    {
                        draw_info_buy = true;
                        button.textBlock.text = "Sell";
                    }
                    DrawPricesWithinInfo(targetList);
                    return true;
                }
            };
            var defaultPrice = 0;
            if (InventoryPatch.LastOver.defaultPrice != null)
                defaultPrice = InventoryPatch.LastOver.defaultPrice.Price;
            var stack = draw_info_buy ? InventoryPatch.LastOver.GetBuyPricesUnder() : InventoryPatch.LastOver.GetSellPricesOver();
            foreach (var store in stack)
            {
                var prices = store.Value;
                if (prices.CanBeBought && !draw_info_buy || !prices.CanBeBought && draw_info_buy) continue;
                string p = $"{prices.Price}$";
                string reputation = (prices.MinReputation.Any()) ? $"{prices.MinReputation.FirstOrDefault().Value}" : "";
                var merchant = TextManager.Get($"storename.{store.Key}").Value;
                if (merchant.Length == 0)
                {
                    merchant = TextManager.Get($"{store.Key}").Value;
                    if (merchant.Length == 0)
                    {
                        merchant = TextManager.Get("store").Value;
                        var typename = store.Key.ToString().Split("merchant")[1];
                        var lt = LocationType.Prefabs.Find(p => (p.Identifier == store.Key || p.Identifier == typename));
                        if (lt != null)
                        {
                            merchant = lt.Name.Value;
                        }
                        //DebugConsole.NewMessage($"Nothing else than store type:{typename} {a}");
                    }

                }
                var color = (draw_info_buy) ? Color.Magenta : Color.Orange;
                var difference = $"‖color:{color}‖{(prices.Price - defaultPrice)}‖color:end‖";
                var textValue = $"{p} ({difference}) {reputation}";
                newText((merchant, textValue));
            }
            DrawAllText(targetList.Content.rectTransform);
            drawInfoAmmo(infosFrameVerticalSplit.Content.rectTransform);
        }

        public static void drawInfoAmmo(RectTransform main)
        {
            List<Identifier> gunsDraw = new();
            var infos = InventoryPatch.ammoInformation();
            if (infos == null) return;
            foreach (var tuples in infos)
                {
                    var gun = tuples.Item1;
                    DebugConsole.NewMessage($"{gun.Name}");
                }
            if(main != null)
            {

                GUIListBox verticalGunList = new(new RectTransform(new Vector2(1f, 0.3f), main), style:null)
                {
                    Spacing = 20
                };
                foreach(var gunTuple in infos)
                {
                    if(gunsDraw.Contains(gunTuple.Item1.Identifier)) continue;
                    
                    GUIListBox horizontalTurretList = new GUIListBox(new RectTransform(new Vector2(1f,1f), verticalGunList.Content.RectTransform), isHorizontal: true)
                    {
                        Spacing = 20
                    };
                    gunTuple.Item1.
                    new GUIImage(new RectTransform(new Vector2(0.2f,1f), horizontalTurretList.Content.rectTransform), gunTuple.Item1.Sprite ?? gunTuple.Item1.InventoryIcon)
                    {
                        color = gunTuple.Item1.SpriteColor
                    };
                    new GUITextBlock(new RectTransform(new Vector2(0.2f, 1f), horizontalTurretList.Content.rectTransform), gunTuple.Item1.Name)
                    {
                        Wrap = true,
                        CanBeFocused = false
                    };
                    new GUITextBlock(new RectTransform(new Vector2(0.5f, 1f), horizontalTurretList.Content.rectTransform), RichString.Rich(gunTuple.Item1.GetTooltip(Character.controlled)))
                    {
                        Wrap = true,
                        CanBeFocused = false
                    };
                    gunsDraw.Add(gunTuple.Item1.Identifier);
                }
            }
        }

    }
}
