using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Barotrauma;
using Barotrauma.Extensions;
using Microsoft.Xna.Framework;
using static Barotrauma.FabricationRecipe;
using static Barotrauma.Networking.MessageFragment;

namespace tooManyBaro.ClientSource
{

    enum GUIToomManyBaro
    {
        recipes,
        deconstructs,
        infos
    }

    class GUI
    {
        public static bool isOpen = false;
        public static GUIToomManyBaro menuopen = GUIToomManyBaro.recipes;

        private static GUILayoutGroup? frame;
        private static GUILayoutGroup? innerFrame;
        private static GUILayoutGroup? mainFrame;
        private static GUIFrame? topFrame;
        private static GUILayoutGroup? itemListFrame;
        private static GUILayoutGroup? paddedItemFrame;
        private static GUILayoutGroup? usageFrame;
        private static GUILayoutGroup? paddedUsageFrame;

        private static List<GUIComponent> allGUIComponents = new();

        public static System.Timers.Timer? _Refresh_multiple_item;

        public static bool draw_info_buy = true;

        /// <summary>
        /// Function to open the UI. Create all the subUI and fill the frames etc..
        /// </summary>
        /// <param name="rectTransform">main target to draw and inherit all the ui. (CharacterHUD.HUDFrame.rectTransform)</param>
        public static void Open(RectTransform rectTransform)
        {
            try
            {
                __unsecure_open(rectTransform);
            }
            catch (Exception ex)
            {
                DebugConsole.NewMessage($"TooManyBaro UI open crash handler : {ex.Message}",color:Color.DarkRed);
                DebugConsole.NewMessage($"TooManyBaro UI open crash handler : {ex.StackTrace}",color:Color.DarkRed);
                string itemName = "[null] ? ";
                if(InventoryPatch.LastOver != null)
                {
                    itemName = InventoryPatch.LastOver.ToString();
                }
                DebugConsole.NewMessage($"TooManyBaro UI open crash handler : item was {itemName}",color:Color.DarkRed);
                DebugConsole.NewMessage($"TooManyBaro UI open crash handler : Open is cancel. (item chosen may have caused this, choose another.)",color:Color.DarkRed);
                Close();
                isOpen = false;
                menuopen = GUIToomManyBaro.recipes;
            }
        }

        private static void __unsecure_open(RectTransform rectTransform)
        {
            if (_Refresh_multiple_item == null)
            {
                _Refresh_multiple_item = new System.Timers.Timer(2000);
                _Refresh_multiple_item.Elapsed += refreshMultipleRequired;
                _Refresh_multiple_item.AutoReset = true;
            }
            Clear();
            isOpen = true;
            //frame = new GUIFrame(new RectTransform(new Vector2(0.5f,0.5f), rectTransform, Anchor.Center));
            frame = new GUILayoutGroup(new RectTransform(new Vector2(0.95f, 0.9f), rectTransform, Anchor.CenterRight), childAnchor: Anchor.TopCenter);
            new GUITextBlock(new RectTransform(new Vector2(1f, 0.05f), frame.RectTransform), "Toomanybaro item craft", font: GUIStyle.SubHeadingFont)
            {
                TextAlignment = Alignment.Center,
                AutoScaleVertical = true
            };

            allGUIComponents.Add(frame);
            innerFrame = new GUILayoutGroup(new RectTransform(new Vector2(1f, 0.95f), frame.RectTransform, Anchor.Center), isHorizontal: true)
            {
                RelativeSpacing = 0.01f,
                Stretch = true,
                CanBeFocused = true
            };
            allGUIComponents.Add(innerFrame);

            mainFrame = new GUILayoutGroup(new RectTransform(Vector2.One, innerFrame.RectTransform), childAnchor: Anchor.TopCenter)
            {
                RelativeSpacing = 0.02f,
                Stretch = true,
                CanBeFocused = true
            };
            allGUIComponents.Add(mainFrame);

            GUIFrame wholeframe = new GUIFrame(new RectTransform(new Vector2(1.0f, 0.65f), mainFrame.RectTransform));
            topFrame = new GUIFrame(new RectTransform(new Vector2(1.0f, 0.9f), wholeframe.RectTransform), style: "InnerFrameDark")
            {
                isSelected = true,
            };
            allGUIComponents.Add(topFrame);

            if (menuopen == GUIToomManyBaro.recipes)
                drawRecipes(wholeframe);
            else if (menuopen == GUIToomManyBaro.deconstructs)
                drawDeconstruct();
            else if (menuopen == GUIToomManyBaro.infos)
            {
                drawInfos();
            }
            else
            {
                drawRecipes(wholeframe);
                menuopen = GUIToomManyBaro.recipes;
            }
            var rightSideButtonList = new GUIListBox(new RectTransform(new Vector2(0.16f, 1f), innerFrame.rectTransform), style: null)
            {
                Spacing = 20
            };
            new GUIListBox(new RectTransform(new Vector2(1f, 0.4f), rightSideButtonList.Content.rectTransform), style: null);
            new GUIButton(new RectTransform(new Vector2(1f, 0.1f), rightSideButtonList.Content.rectTransform)
            {
                //RelativeOffset = new Vector2(0f, 0.05f)
            }, "Close")
            {
                OnClicked = (GUIButton button, object obj) =>
                {
                    Close();

                    return true;
                }
            };

            string textToggleDecraft = (menuopen == GUIToomManyBaro.recipes) ? "Deconstruct recipes" : "Recipes Usages";

            //swap ui
            new GUIButton(new RectTransform(new Vector2(1f, 0.1f), rightSideButtonList.Content.rectTransform)
            {
                //RelativeOffset = new Vector2(0.05f,0f)
            }, textToggleDecraft)
            {
                OnClicked = (GUIButton button, object obj) =>
                {
                    if (menuopen == GUIToomManyBaro.recipes)
                        menuopen = GUIToomManyBaro.deconstructs;
                    else
                        menuopen = GUIToomManyBaro.recipes;
                    Open(CharacterHUD.HUDFrame.rectTransform);
                    return true;
                }
            };
            new GUIButton(new RectTransform(new Vector2(1f, 0.1f), rightSideButtonList.Content.rectTransform)
            {
                //RelativeOffset = new Vector2(0.05f,0f)
            }, "Item infos")
            {
                OnClicked = (GUIButton button, object obj) =>
                {
                    menuopen = GUIToomManyBaro.infos;
                    Open(CharacterHUD.HUDFrame.rectTransform);
                    return true;
                }
            };
            _Refresh_multiple_item.Start();
        }

        public static void drawInfos()
        {
            if (topFrame == null) return;
            if (InventoryPatch.LastOver == null) return;

            var infosPadded = new GUIFrame(new RectTransform(new Vector2(0.96f, 0.96f), topFrame.rectTransform, Anchor.Center), style: "itemUI");
            var infosFrame = new GUILayoutGroup(new RectTransform(new Vector2(0.9f, 0.9f), infosPadded.rectTransform, anchor: Anchor.Center));
            var infosFrameVerticalSplit = new GUIListBox(new RectTransform(new Vector2(1f, 1f), infosFrame.rectTransform), style: null)
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
            if(icon != null)
                new GUIImage(new RectTransform(new Vector2(1f, 1f), leftSideFrame.rectTransform, anchor: Anchor.Center), icon);
            new GUIFrame(new RectTransform(new Vector2(0.01f, 0.6f), infosFrameHorizontalSplitList.Content.RectTransform), style: "VerticalLine");
            // Right side for the tooltip
            var rightSideFrame = new GUILayoutGroup(new RectTransform(new Vector2(0.6f, 1f), infosFrameHorizontalSplitList.Content.rectTransform));
            new GUITextBlock(new RectTransform(new Vector2(1f, 1f), rightSideFrame.rectTransform), RichString.Rich(InventoryPatch.LastOver.GetTooltip(Character.controlled)));

            // Bottom side, diverse infos.

            // Price: 
            var priceFrameNotScrollable = new GUIFrame(new RectTransform(new Vector2(1f, 0.6f), infosFrameVerticalSplit.Content.rectTransform), style: null);
            var priceFrame = new GUIListBox(new RectTransform(new Vector2(1f, 1f), priceFrameNotScrollable.rectTransform),style: null);
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

            


            /// <summary>generate a list of text block from the allTexts list. Can be supplied with custom expression to generate the text to fill from the list.</summary>
            ///<example>drawAllText(priceFrameBooleanVerticalSplit.Content.rectTransform,
            ///id => { return string.Concat(id, string.Concat(Enumerable.Repeat(" ", (bigger - id.Length)))); },
            ///    (id, text) => { return $"{id}: ‖color:{Color.Gold}‖{text}‖color:end‖"; }
            ///    );
            ///</ example>
            
            var needUnlock = false;
            if (InventoryPatch.LastOver.DefaultPrice != null)
                needUnlock = InventoryPatch.LastOver.DefaultPrice.RequiresUnlock;
            newText(("Can be Bought", addBoolRichString(InventoryPatch.LastOver.CanBeBought).ToString()));
            newText(("Can be Sold", addBoolRichString(InventoryPatch.LastOver.CanBeSold).ToString()));
            newText(("Need unlock", addBoolRichString(needUnlock).ToString()));
            newText(("Could you buy", addBoolRichString(InventoryPatch.LastOver.CanCharacterBuy() && InventoryPatch.LastOver.CanBeBought).ToString()));
            drawAllTextN(priceFrameBooleanVerticalSplit.Content.rectTransform);
            bigger = 0;
            allTexts.Clear();
            newText(("Price", $"{InventoryPatch.LastOver.defaultPrice.Price}"));
            newText(("Buying Factor", $"{InventoryPatch.LastOver.defaultPrice.BuyingPriceMultiplier}x"));
            newText(("Min Price", $"{InventoryPatch.LastOver.GetMinPrice()}"));
            newText(("faction", $"{faction}"));
            newText(("Reputation", $"{reputationNeeded}"));
            drawAllTextN(priceFramePriceValuesVerticalSplit.Content.rectTransform);

            //foreach (var tuple in allTexts)
            //{
            //    String id = string.Concat(tuple.Item1, string.Concat(Enumerable.Repeat(" ", (bigger - tuple.Item1.Length))));
            //    RichString text = $"{id}: ‖color:{Color.Gold}‖{tuple.Item2}‖color:end‖";
            //    new GUITextBlock(new RectTransform(new Vector2(1f, 0.1f), priceFramePriceValuesVerticalSplit.Content.rectTransform), RichString.Rich(text))
            //    {
            //        CanBeFocused = false,
            //        Font = GUIStyle.MonospacedFont,
            //        TextSize = new Vector2(3f, 3f)
            //    };
            //}



            //foreach (var tuple in allTexts)
            //{
            //    String id = string.Concat(tuple.Item1, string.Concat(Enumerable.Repeat(" ", (bigger - tuple.Item1.Length))));
            //    RichString text = $"{id}: ‖color:{Color.Gold}‖{tuple.Item2}‖color:end‖";
            //    new GUITextBlock(new RectTransform(new Vector2(1f, 0.1f), pricePerMerchantFramePriceValuesVerticalSplit.Content.rectTransform), RichString.Rich(text))
            //    {
            //        CanBeFocused = false,
            //        Font = GUIStyle.MonospacedFont,
            //        TextSize = new Vector2(3f, 3f)
            //    };
            //}
            drawPricesWithinInfo(pricePerMerchantFramePriceValuesVerticalSplit);

        }
        private static List<(String, String)> allTexts = new();
        private static int bigger = 0;
        private static void newText((string, string) t)
        {
            if (t.Item1.Length > bigger) bigger = t.Item1.Length;
            allTexts.Add(t);
        }
        private static void drawAllText(RectTransform target, Func<String, String>? IdExpression, Func<String, String, String>? TextExpression)
        {
            if (IdExpression == null) IdExpression = id => { return string.Concat(id, string.Concat(Enumerable.Repeat(" ", (bigger - id.Length)))); };
            if (TextExpression == null) TextExpression = (id, text) => { return $"{id}: ‖color:{Color.Gold}‖{text}‖color:end‖"; };

            foreach (var tuple in allTexts)
            {
                String id = IdExpression(tuple.Item1);// string.Concat(tuple.Item1, string.Concat(Enumerable.Repeat(" ", (bigger - tuple.Item1.Length))));
                RichString text = TextExpression(tuple.Item1, tuple.Item2);//$"{id}: ‖color:{Color.Gold}‖{tuple.Item2}‖color:end‖";
                new GUITextBlock(new RectTransform(new Vector2(1f, 0.1f), target), RichString.Rich(text))
                {
                    CanBeFocused = false,
                    Font = GUIStyle.MonospacedFont,
                    TextSize = new Vector2(3f, 3f)
                };
            }
        }
        private static void drawAllTextN(RectTransform target) { drawAllText(target, null, null); }

        private static void drawPricesWithinInfo(GUIListBox targetList)
        {
            targetList.ClearChildren();
            if (InventoryPatch.LastOver == null) return;
            var buttonSwapPriceBuyOrSellf = new GUIButton(new RectTransform(new Vector2(1f, 0.1f), targetList.Content.rectTransform)
            {
                //RelativeOffset = new Vector2(0f, -0.2f)
            }, (draw_info_buy)?"Sell":"Buy")
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
                        draw_info_buy= true;
                        button.textBlock.text = "Sell";
                    }
                    drawPricesWithinInfo(targetList);
                    return true;
                }
            };
            allTexts.Clear();
            DebugConsole.NewMessage($"draw {draw_info_buy}");
            var stack = draw_info_buy ? InventoryPatch.LastOver.GetBuyPricesUnder() : InventoryPatch.LastOver.GetSellPricesOver();
            foreach (var store in stack)
            {
                var prices = store.Value;
                if (prices.CanBeBought && !draw_info_buy|| !prices.CanBeBought && draw_info_buy) continue;
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
                var textValue = $"{p} {prices.BuyingPriceMultiplier}x {reputation}";
                newText((merchant, textValue));
            }
            drawAllTextN(targetList.Content.rectTransform);
        }


        //  color symbol ‖
        public static RichString addBoolRichString(bool B, string text = "")
        {
            var cfalse = GUIStyle.ColorReputationVeryLow.Value;
            var ctrue = GUIStyle.ColorReputationVeryHigh.Value;
            var c = B ? ctrue : cfalse;
            var t = B ? "true" : "false";
            return $"{text}‖color:{c}‖{t}‖color:end‖";
        }

        public static void drawDeconstruct()
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
                        imageToItem.Add((img, item));
                        var pItems = new GUIListBox(new RectTransform(new Vector2(0.7f, 1f), il.Content.rectTransform), style: null, isHorizontal: true);
                        AddDeconstructToList(item.DeconstructItems.ToList(), pItems.Content.rectTransform);
                    }
            }
        }

        public static void drawRecipes(GUIFrame wholeframe)
        {
            if (topFrame == null) return;
            // PRODUCERS SIDE
            itemListFrame = new GUILayoutGroup(new RectTransform(new Vector2(0.5f, 1.0f), topFrame.RectTransform), childAnchor: Anchor.Center);
            var itemList = new GUIListBox(new RectTransform(new Vector2(1f, 0.9f), itemListFrame.RectTransform), style: null);
            paddedItemFrame = new GUILayoutGroup(new RectTransform(new Vector2(0.9f, 0.9f), itemListFrame.RectTransform));
            new GUITextBlock(new RectTransform(new Vector2(1f, 0.04f), itemList.Content.RectTransform), "List of Recipe as Target", textAlignment: Alignment.Center)
            {
                isSelected = false,
                CanBeFocused = false,
            };

            // SEPARATOR FOR LEFT AND RIGHT
            new GUIFrame(new RectTransform(new Vector2(0.01f, 0.9f), topFrame.RectTransform, Anchor.Center), style: "VerticalLine");

            // USAGES SIDE

            usageFrame = new GUILayoutGroup(new RectTransform(new Vector2(0.5f, 1f), topFrame.RectTransform, Anchor.TopRight), childAnchor: Anchor.Center);
            var itemListUsage = new GUIListBox(new RectTransform(new Vector2(1f, 0.9f), usageFrame.RectTransform), style: null);
            paddedUsageFrame = new GUILayoutGroup(new RectTransform(new Vector2(0.95f, 0.9f), usageFrame.RectTransform));
            new GUITextBlock(new RectTransform(new Vector2(1f, 0.04f), itemListUsage.Content.RectTransform), "List Of Recipe as Ingredient", textAlignment: Alignment.Center)
            {
                isSelected = false,
                CanBeFocused = false,
            };

            //DECONSTRUCT OUTPUT BOTTOM
            var deconsframe = new GUILayoutGroup(new RectTransform(new Vector2(1f, 0.1f), wholeframe.RectTransform, anchor: Anchor.BottomCenter), childAnchor: Anchor.Center);
            var deconsList = new GUIListBox(new RectTransform(new Vector2(0.25f, 1f), deconsframe.RectTransform), style: null);
            new GUITextBlock(new RectTransform(new Vector2(1f, 0.1f), deconsList.Content.RectTransform), "Output When Deconstruct", textAlignment: Alignment.Center)
            {
                isSelected = false,
                CanBeFocused = false,
            };
            var deconsListSub = new GUIFrame(new RectTransform(new Vector2(1f, 0.9f), deconsList.Content.rectTransform), style: null);
            allGUIComponents.Add(wholeframe);
            allGUIComponents.Add(deconsframe);
            allGUIComponents.Add(deconsList);
            AddRecipesToListFilter(InventoryPatch.Producers, itemList.Content.rectTransform, InventoryPatch.ProduceWhenDeconstruct);
            AddRecipesToListFilter(InventoryPatch.Usages, itemListUsage.Content.rectTransform);
            AddDeconstructToList(InventoryPatch.DeconstructItems, deconsListSub.rectTransform);
            allGUIComponents.Add(itemListFrame);
            allGUIComponents.Add(paddedItemFrame);
            allGUIComponents.Add(usageFrame);
        }

        /// <summary>
        /// Similar to <seealso cref="AddRecipesToList(List{FabricationRecipe}, RectTransform)"/> this function 
        /// draw the result of deconstructing the object.
        /// </summary>
        /// <param name="deconstructItems">List of item given for the destruction of the object</param>
        /// <param name="rectTransform">Target to draw to.</param>
        public static void AddDeconstructToList(List<DeconstructItem> deconstructItems, RectTransform rectTransform)
        {
            GUIListBox itemlist = new GUIListBox(new RectTransform(new Vector2(1f, 1f), rectTransform), isHorizontal: true);
            foreach (var item in deconstructItems)
            {
                var ip = (ItemPrefab)ItemPrefab.FindByIdentifier(item.ItemIdentifier);
                var itemicon = ip.InventoryIcon;
                if (itemicon != null)
                {
                    var imgFrame = new GUIFrame(new RectTransform(new Vector2(0.2f, 1f), itemlist.Content.rectTransform), style: null);
                    RichString tooltip = ((ItemPrefab)ip).GetTooltip(Character.controlled);
                    GUIImage img = new GUIImage(new RectTransform(new Vector2(1f, 1f), imgFrame.rectTransform), itemicon, scaleToFit: true)
                    {
                        color = ip.SpriteColor,
                        toolTip = RichString.Rich(tooltip)
                    };
                    imageToItem.Add((img, ItemPrefab.GetItemPrefab(ip.Identifier.value)));
                    new GUITextBlock(new RectTransform(new Vector2(0f, 1f), itemlist.Content.rectTransform), $"x{item.Amount}", textAlignment: Alignment.BottomRight);
                }
            }
            allGUIComponents.Add(itemlist);

        }

        public static List<(GUIImage, ItemPrefab)> imageToItem = new();


        public static void AddRecipesToListFilter(List<FabricationRecipe> l, RectTransform targetRect, List<ItemPrefab>? producewhenDecons = null)
        {
            var lrecipeNeeded = (List<FabricationRecipe>)l.Where(static r => { return r.RequiresRecipe == true; }).ToList();
            var paddednrnf = new GUIListBox(new RectTransform(new Vector2(1f, 0.7f), targetRect), style: "ItemUI");
            var noRecipeNeededFrame = new GUIListBox(new RectTransform(new Vector2(0.9f, 0.9f), paddednrnf.Content.rectTransform, anchor: Anchor.Center), style: null);
            AddRecipesToList((List<FabricationRecipe>)l.Where(static r => { return r.RequiresRecipe == false; }).ToList(), noRecipeNeededFrame.Content.rectTransform);
            new GUIFrame(new RectTransform(new Vector2(0.9f, 0.01f), targetRect), style: "HorizontalLine");
            if (lrecipeNeeded.Count <= 0) return;
            new GUITextBlock(new RectTransform(new Vector2(1f, 0.05f), targetRect), "Require a Recipe", textAlignment: Alignment.Center)
            {
                textColor = GUIStyle.TextColorBright,
                isSelected = false,
                CanBeFocused = false,
            };
            var paddedrnf = new GUIListBox(new RectTransform(new Vector2(1f, 0.7f), targetRect), style: "ItemUI");
            var RecipeNeededFrame = new GUIListBox(new RectTransform(new Vector2(0.9f, 0.9f), paddedrnf.Content.rectTransform, anchor: Anchor.Center), style: null);
            AddRecipesToList(lrecipeNeeded, RecipeNeededFrame.Content.rectTransform);
        }

        static public Color getItemConditionColor(float value)
        {
            return ((value > .70) ? GUIStyle.ColorReputationHigh : (value > .30) ? GUIStyle.ColorReputationNeutral : GUIStyle.ColorReputationVeryLow);
        }

        /// <summary>
        /// We go through the list of recipe given, and add them to the HUD object given.
        /// Same function for the Producer and Usages list.
        /// </summary>
        /// <param name="l">List of recipes</param>
        /// <param name="targetRect">Target to draw to.</param>
        public static void AddRecipesToList(List<FabricationRecipe> l, RectTransform targetRect)
        {
            if (l == null || targetRect == null) return;

            foreach (var recipe in l)
            {
                if (recipe == null || !(recipe.RequiredItems.Count() > 0))
                {
                    continue;
                }

                // symbole '‖'
                GUIListBox recipeLine = new GUIListBox(new RectTransform(new Vector2(1f, 0.2f), targetRect), isHorizontal: true, style: null);
                GUIListBox lrecipe = new GUIListBox(new RectTransform(new Vector2(0.5f, 1f), recipeLine.Content.rectTransform), isHorizontal: true, style: null);
                GUIListBox rrecipe = new GUIListBox(new RectTransform(new Vector2(0.5f, 1f), recipeLine.Content.rectTransform), isHorizontal: true, style: null);
                foreach (FabricationRecipe.RequiredItem ip in recipe.RequiredItems)
                {
                    ItemPrefab? curr_item_draw = null;
                    bool needToBeSwap = false;
                    float iconAlpha = 1f;
                    if (ip.ItemPrefabs.Multiple())
                    {
                        needToBeSwap = true;
                        curr_item_draw = ip.ItemPrefabs.FirstOrDefault();
                        float iconCycleSpeed = 0.75f;
                        float iconCycleT = (float)Timing.TotalTime * iconCycleSpeed;
                        int iconIndex = (int)(iconCycleT % ip.ItemPrefabs.Count());

                        iconAlpha = Math.Min(Math.Abs(MathF.Sin(iconCycleT * MathHelper.Pi)) * 2.0f, 1.0f);
                    }
                    else
                    {
                        curr_item_draw = ip.ItemPrefabs.FirstOrDefault();
                    }
                    if (curr_item_draw == null) continue;
                    var itemIcon = curr_item_draw.InventoryIcon ?? curr_item_draw.Sprite;
                    if (itemIcon != null)
                    {
                        var itemIconframe = new GUIFrame(new RectTransform(new Vector2(0.2f, 1f), lrecipe.Content.rectTransform), style: null);
                        GUILayoutGroup itemDeconsIconBox = new GUILayoutGroup(new RectTransform(new Vector2(1f, 1f), itemIconframe.rectTransform, anchor: Anchor.Center), childAnchor: Anchor.Center);


                        // ITEM CONDITION CHECKING
                        // drawing after to not be under the item.
                        String conditionTooltip = getConditionTooltip(ip);
                        bool needCondition = conditionTooltip.Length > 0;

                        RichString tooltip = conditionTooltip + curr_item_draw.GetTooltip(Character.controlled);
                        var img = new GUIImage(new RectTransform(new Vector2(1f, 1.0f), itemDeconsIconBox.rectTransform), itemIcon, scaleToFit: true)
                        {
                            Color = curr_item_draw.InventoryIconColor,
                            toolTip = RichString.Rich(tooltip),
                        };
                        if (needToBeSwap)
                            item_swap_over_time.Add((img, ip));
                        // WRITE CONDITION OVER ITEM
                        if (needCondition)
                        {
                            var cmin = getItemConditionColor(ip.MinCondition);
                            var cmax = getItemConditionColor(ip.MaxCondition);
                            var barFrame = new GUIFrame(new RectTransform(new Vector2(0.8f, 0.1f), itemIconframe.rectTransform, anchor: Anchor.BottomCenter), style: null);
                            new GUIProgressBar(new RectTransform(new Vector2(1f, 1f), barFrame.rectTransform, anchor: Anchor.BottomCenter), (ip.MinCondition > ip.MaxCondition) ? ip.MinCondition : ip.MaxCondition, color: cmin)
                            {
                                isHorizontal = true,
                                CanBeFocused = false,
                            };
                            //new GUITextBlock(new RectTransform(new Vector2(0f, 1f), lrecipe.Content.rectTransform), $"{ip.MinCondition * 100}", font: GUIStyle.SmallFont, textColor: cmin, textAlignment: Alignment.CenterRight);
                            //new GUITextBlock(new RectTransform(new Vector2(0f, 1f), lrecipe.Content.rectTransform), $"{ip.MaxCondition * 100}", font: GUIStyle.SmallFont, textColor: cmax, textAlignment: Alignment.TopRight);
                        }
                        new GUITextBlock(new RectTransform(new Vector2(1f, 0.2f), itemIconframe.rectTransform, anchor: Anchor.BottomRight), $"x{ip.Amount}", textColor: GUIStyle.TextColorBright.Value)
                        {
                            CanBeFocused = false,
                        };
                        imageToItem.Add((img, curr_item_draw));
                    }
                }
                var outputIcon = recipe.TargetItem.InventoryIcon ?? recipe.TargetItem.Sprite;
                if (outputIcon != null)
                {
                    new GUITextBlock(new RectTransform(new Vector2(0.8f, 1f), rrecipe.Content.rectTransform), $"", textAlignment: Alignment.BottomRight)
                    {
                        isSelected = false,
                        CanBeFocused = false,
                    };
                    RichString tooltip = recipe.TargetItem.GetTooltip(Character.controlled);//recipe.TargetItem.CreateTooltipText();
                    var img = new GUIImage(new RectTransform(new Vector2(0.2f, 1.0f), rrecipe.Content.rectTransform), outputIcon, scaleToFit: true)
                    {
                        Color = recipe.TargetItem.InventoryIconColor,
                        toolTip = RichString.Rich(tooltip)
                    };
                    imageToItem.Add((img, recipe.TargetItem));
                    new GUITextBlock(new RectTransform(new Vector2(0.0f, 1f), rrecipe.Content.rectTransform), $"x{recipe.Amount}", textAlignment: Alignment.BottomRight);
                }
                allGUIComponents.Add(recipeLine);
                allGUIComponents.Add(lrecipe);
                allGUIComponents.Add(rrecipe);
            }
        }
        static public String getConditionTooltip(RequiredItem ip)
        {
            string conditionTooltip = "";
            if (ip.MinCondition != ip.MaxCondition)
            {
                var tColor = XMLExtensions.ToStringHex(GUIStyle.InteractionLabelColor);
                var cmin = getItemConditionColor(ip.MinCondition);
                var cmax = getItemConditionColor(ip.MaxCondition);
                conditionTooltip = $"‖color:{tColor}‖Item need to have condition between:‖color:end‖\n‖color:{cmin}‖{ip.MinCondition * 100}‖color:end‖ - ‖color:{cmax}‖{ip.MaxCondition * 100}‖color:end‖\n";
            }
            return conditionTooltip;
        }

        static public List<(GUIImage, RequiredItem)> item_swap_over_time = new();

        static int _refreshIconIndex = 0;
        static public void refreshMultipleRequired(Object source, ElapsedEventArgs e)
        {
            _refreshIconIndex++;
            if (item_swap_over_time.Count > 0)
            {
                foreach (var tuple in item_swap_over_time)
                {
                    int iconIndex = (int)(_refreshIconIndex % tuple.Item2.ItemPrefabs.Count());

                    var requiredItemToDisplay = tuple.Item2.ItemPrefabs.Skip(iconIndex).FirstOrDefault();
                    var img = requiredItemToDisplay?.InventoryIcon ?? requiredItemToDisplay?.Sprite;
                    if (requiredItemToDisplay != null && img != null)
                    {
                        tuple.Item1.UserData = requiredItemToDisplay;
                        tuple.Item1.toolTip = RichString.Rich(getConditionTooltip(tuple.Item2) + requiredItemToDisplay.GetTooltip(Character.controlled));
                        tuple.Item1.Sprite = img;
                    }
                }
            }
        }

        public static void Close()
        {
            Clear();
            isOpen = false;
            InventoryPatch.swapSave.Clear();
        }

        /// <summary>
        /// Clear the UI, we also clear all the gui component. Why ? because all the exemples i could find did something
        /// similar and it was not working unless i do that. Breaking the order would also provoke null exception somewhere.
        /// </summary>
        public static void Clear()
        {
            _Refresh_multiple_item?.Stop();
            item_swap_over_time.Clear();
            //InventoryPatch.swapped = false;
            for (var i = 0; i < allGUIComponents.Count; i++)
            {
                GUIComponent comp = allGUIComponents.ElementAt(i);
                if (comp != null)
                {
                    comp?.Parent?.RemoveChild(comp);
                    comp?.ClearChildren();
                    //comp = null;
                }
            }
            allGUIComponents.Clear();
            imageToItem.Clear();
        }

        /// <summary>
        /// Choose the action to do upon event of the mouse.
        /// </summary>
        /// <param name="isLeftClick">if true then it's left click that was clicked. False means right click.</param>
        public static void mouseClicked(bool isLeftClick)
        {
            if (!isOpen) return;
            if (!isLeftClick)
            {
                if (InventoryPatch.swapped)
                    InventoryPatch.unswapSubItem();
                else
                    Close();
                return;
            }
            ItemPrefab toswapto = null;
            foreach (var comp in imageToItem)
            {
                if (comp.Item1.rectTransform.Rect.Contains(PlayerInput.MousePosition))
                {
                    toswapto = ((ItemPrefab)comp.Item1.UserData) ?? comp.Item2;
                    break;
                }
            }
            if (toswapto != null)
                InventoryPatch.swapSubItem(toswapto);
        }
    }

}
