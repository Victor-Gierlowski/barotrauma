using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Timers;
using Barotrauma;
using Barotrauma.Abilities;
using Barotrauma.Extensions;
using EventInput;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Mono.Cecil;
using static Barotrauma.FabricationRecipe;
using static Barotrauma.PetBehavior.ItemProduction;
using Item = Barotrauma.Item;



namespace tooManyBaro
{
    class toomanybaro : IAssemblyPlugin
    {
        public Harmony harmony;
        public void Initialize()
        {
            harmony = new Harmony("com.barotrauma.toomanybaro");
            toomanybaroKeyBoardInput myInputs = new toomanybaroKeyBoardInput();
            Barotrauma.DebugConsole.NewMessage("created a harmony",Color.Gold);
            // Utiliser AccessTools pour obtenir la méthode privée
            var originalMethod = AccessTools.Method(typeof(Inventory), "UpdateSlot");
            if (originalMethod != null)
            {
                harmony.Patch(
                    original: originalMethod,
                    postfix: new HarmonyMethod(typeof(InventoryPatch).GetMethod("UpdateSlotPostfix"))
                );
            }
            else
            {
                DebugConsole.NewMessage("Protected method UpdateSlot not found", Color.Red);
            }
            Barotrauma.DebugConsole.NewMessage("Loaded MouseInventory", Color.Gold);


            harmony.Patch(
                original: typeof(PlayerInput).GetMethod("Update"),
                postfix: new HarmonyMethod(typeof(toomanybaroKeyBoardInput).GetMethod("onUpdateKeys"))
                );
            Barotrauma.DebugConsole.NewMessage("Loaded updateKeys Settings Menu", Color.Gold);

        }
        public void OnLoadCompleted() {
            foreach (ItemPrefab iprefab in ItemPrefab.Prefabs)
            {

                //string textdec = "";
                //if (iprefab.AllowDeconstruct)
                //{
                //    textdec += $"{iprefab.Name} will produce upon deconstruction : ";
                //    foreach (var item in iprefab.DeconstructItems)
                //    {
                //        textdec += $" {ItemPrefab.FindByIdentifier(item.ItemIdentifier).Name}x{item.Amount} ; ";
                //    }
                //    textdec += "\n";
                //}
                //DebugConsole.NewMessage(textdec,Color.Snow);
                foreach(FabricationRecipe recipe in iprefab.FabricationRecipes.Values)
                    InventoryPatch.allRecipes.Add(recipe);
                
            }
            DebugConsole.NewMessage($"Found {InventoryPatch.allRecipes.Count} recipes ! ");
            //var col = GUIComponentStyle;
            //DebugConsole.NewMessage($"il y'a {col.Count()} styles");
            //foreach(var i in col)
            //{
            //    DebugConsole.NewMessage($"{i.Name} - {i.ToString()}\n");
            //}
        }
        public void PreInitPatching() { }
        public void Dispose()
        {
            harmony.UnpatchAll();
            harmony = null;
        }

    }

    class toomanybaroKeyBoardInput
    {
        /// <summary>
        /// Because i couldn't find any simpler way to listen for keyboard input.
        /// After the core game update, I check too for my inputs.
        /// </summary>
        /// <param name="deltaTime"></param>
        public static void onUpdateKeys(double deltaTime)
        {
            Keys[] kPressed = PlayerInput.keyboardState.GetPressedKeys();
            foreach(Keys key in kPressed)
            {
                if (key == Keys.O)
                    InventoryPatch.checkInput();
            }
            var lftclick = PlayerInput.PrimaryMouseButtonClicked();
            var rghclick = PlayerInput.SecondaryMouseButtonClicked();
            if (lftclick || rghclick)
                GUI.mouseClicked(lftclick);
        }
    }

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

        /// <summary>
        /// Function to open the UI. Create all the subUI and fill the frames etc..
        /// </summary>
        /// <param name="rectTransform">main target to draw and inherit all the ui. (CharacterHUD.HUDFrame.rectTransform)</param>
        public static void Open(RectTransform rectTransform)
        {
            if(_Refresh_multiple_item == null)
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
            innerFrame = new GUILayoutGroup(new RectTransform(new Vector2(1f, 0.95f), frame.RectTransform, Anchor.Center), isHorizontal: true)
            {
                RelativeSpacing = 0.01f,
                Stretch = true,
                CanBeFocused = true
            };

            mainFrame = new GUILayoutGroup(new RectTransform(Vector2.One, innerFrame.RectTransform), childAnchor: Anchor.TopCenter)
            {
                RelativeSpacing = 0.02f,
                Stretch = true,
                CanBeFocused = true
            };

            GUIFrame wholeframe = new GUIFrame(new RectTransform(new Vector2(1.0f, 0.65f), mainFrame.RectTransform));
            topFrame = new GUIFrame(new RectTransform(new Vector2(1.0f, 0.9f), wholeframe.RectTransform), style: "InnerFrameDark")
            {
                isSelected = true,
            };

            if(menuopen == GUIToomManyBaro.recipes)
                drawRecipes(wholeframe);
            else if(menuopen == GUIToomManyBaro.deconstructs)
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
            allGUIComponents.Add(frame);
            allGUIComponents.Add(innerFrame);
            allGUIComponents.Add(mainFrame);
            allGUIComponents.Add(topFrame);
            _Refresh_multiple_item.Start();
        }

        public static void drawInfos()
        {
            if (topFrame == null) return;
            if (InventoryPatch.LastOver == null) return;

            var infosPadded = new GUIFrame(new RectTransform(new Vector2(0.96f, 0.96f), topFrame.rectTransform, Anchor.Center), style:"itemUI");
            var infosFrame = new GUILayoutGroup(new RectTransform(new Vector2(0.9f, 0.9f), infosPadded.rectTransform, anchor:Anchor.Center));
            var infosFrameVerticalSplit = new GUIListBox(new RectTransform(new Vector2(1f, 1f),infosFrame.rectTransform), style: null)
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
            new GUIImage(new RectTransform(new Vector2(1f, 1f), leftSideFrame.rectTransform, anchor:Anchor.Center), InventoryPatch.LastOver.InventoryIcon ?? InventoryPatch.LastOver.Sprite);
            new GUIFrame(new RectTransform(new Vector2(0.01f, 0.6f), infosFrameHorizontalSplitList.Content.RectTransform), style: "VerticalLine");
            // Right side for the tooltip
            var rightSideFrame = new GUILayoutGroup(new RectTransform(new Vector2(0.6f, 1f), infosFrameHorizontalSplitList.Content.rectTransform));
            new GUITextBlock(new RectTransform(new Vector2(1f, 1f), rightSideFrame.rectTransform), RichString.Rich(InventoryPatch.LastOver.GetTooltip(Character.controlled)));

            // Bottom side, diverse infos.

            // Price: 
            var priceFrame = new GUIFrame(new RectTransform(new Vector2(1f, 0.4f), infosFrameVerticalSplit.Content.rectTransform), style: null);
            var priceFrameHorizontaleSplit = new GUIListBox(new RectTransform(new Vector2(1f, 1f), priceFrame.rectTransform), isHorizontal:true ,style:null);
            var priceFrameBooleanVerticalSplit = new GUIListBox(new RectTransform(new Vector2(0.3f, 1f), priceFrameHorizontaleSplit.Content.rectTransform), style:null)
            {
                Spacing = 20
            };
            //new GUITextBlock(new RectTransform(new Vector2(1f, 0.1f), priceFrameBooleanVerticalSplit.Content.rectTransform), RichString.Rich(addBoolRichString(InventoryPatch.LastOver.CanBeBought, "Can be Bought :")))
            //{
            //    Font=GUIStyle.LargeFont
            //};
            //new GUITextBlock(new RectTransform(new Vector2(1f, 0.1f), priceFrameBooleanVerticalSplit.Content.rectTransform), RichString.Rich(addBoolRichString(InventoryPatch.LastOver.CanBeSold, "Can be Sold :")))
            //{
            //    Font = GUIStyle.LargeFont
            //}; ;
            //new GUITextBlock(new RectTransform(new Vector2(1f, 0.1f), priceFrameBooleanVerticalSplit.Content.rectTransform), RichString.Rich(addBoolRichString(InventoryPatch.LastOver.DefaultPrice.RequiresUnlock, "Need unlock:")))
            //{
            //    Font = GUIStyle.LargeFont
            //}; ;
            //new GUITextBlock(new RectTransform(new Vector2(1f, 0.1f), priceFrameBooleanVerticalSplit.Content.rectTransform), RichString.Rich(addBoolRichString(InventoryPatch.LastOver.CanCharacterBuy(), "Could you buy:")))
            //{
            //    Font = GUIStyle.LargeFont
            //}; ;

            var priceFramePriceValuesVerticalSplit = new GUIListBox(new RectTransform(new Vector2(0.3f, 1f), priceFrameHorizontaleSplit.Content.rectTransform), style: null)
            {
                Spacing = 20
            };
            var pricePerMerchantFramePriceValuesVerticalSplit = new GUIListBox(new RectTransform(new Vector2(0.3f, 1f), priceFrameHorizontaleSplit.Content.rectTransform), style: null)
            {
                Spacing = 20
            };
            string reputationNeeded = "";
            string faction = FactionPrefab.Prefabs.Find(f => f.Identifier == InventoryPatch.LastOver.DefaultPrice.RequiredFaction)?.Name.ToString()??"";

            float reput = -1;
            //string marchant = "";
            var e = InventoryPatch.LastOver.DefaultPrice;
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
                    foreach(var b in a.Value.MinReputation)
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
            if(reput!=-1)
                reputationNeeded += $"[‖color:{GUIStyle.ColorReputationVeryHigh.Value}‖{reput}‖color:end‖]";

            List<(String, String)> allTexts = new();
            int bigger = 0;
            void newText((string, string) t){
                if(t.Item1.Length > bigger) bigger = t.Item1.Length;
                allTexts.Add(t);
            }
            newText(("Can be Bought",addBoolRichString(InventoryPatch.LastOver.CanBeBought).ToString()));
            newText(("Can be Sold",addBoolRichString(InventoryPatch.LastOver.CanBeSold).ToString()));
            newText(("Need unlock",addBoolRichString(InventoryPatch.LastOver.DefaultPrice.RequiresUnlock).ToString()));
            newText(("Could you buy",addBoolRichString(InventoryPatch.LastOver.CanCharacterBuy() && InventoryPatch.LastOver.CanBeBought).ToString()));

            foreach (var tuple in allTexts)
            {
                String id = string.Concat(tuple.Item1, string.Concat(Enumerable.Repeat(" ", (bigger - tuple.Item1.Length))));
                RichString text = $"{id}: ‖color:{Color.Gold}‖{tuple.Item2}‖color:end‖";
                new GUITextBlock(new RectTransform(new Vector2(1f, 0.1f), priceFrameBooleanVerticalSplit.Content.rectTransform), RichString.Rich(text))
                {
                    CanBeFocused = false,
                    Font = GUIStyle.MonospacedFont,
                    TextSize = new Vector2(3f, 3f)
                };
            }
            bigger = 0;
            allTexts.Clear();
            newText(("Price", $"{InventoryPatch.LastOver.defaultPrice.Price}"));
            newText(("Buying Factor", $"{InventoryPatch.LastOver.defaultPrice.BuyingPriceMultiplier}x"));
            newText(("Min Price", $"{InventoryPatch.LastOver.GetMinPrice()}"));
            newText(("faction", $"{faction}"));
            newText(("Reputation", $"{reputationNeeded}"));
            foreach (var tuple in allTexts){
                String id = string.Concat(tuple.Item1,string.Concat(Enumerable.Repeat(" ",(bigger - tuple.Item1.Length))));
                RichString text = $"{id}: ‖color:{Color.Gold}‖{tuple.Item2}‖color:end‖";
                new GUITextBlock(new RectTransform(new Vector2(1f, 0.1f), priceFramePriceValuesVerticalSplit.Content.rectTransform), RichString.Rich(text))
                {
                    CanBeFocused=false,
                    Font = GUIStyle.MonospacedFont,
                    TextSize = new Vector2(3f, 3f)
                }; 
            }

            allTexts.Clear();
            foreach(var store in InventoryPatch.LastOver.StorePrices)
            {
                var prices = store.Value;
                if (!prices.CanBeBought) continue;
                string p =$"{prices.Price}$";
                var el = MapEntityPrefab.FindByIdentifier(store.Key);
                CharacterPrefab.Prefabs.Any(cp => cp.Identifier == store.Key);
                if (el != null)
                {
                    DebugConsole.NewMessage($"found and {el.Category.ToString()}");
                }
                string reputation = (prices.MinReputation.Any()) ? $"{prices.MinReputation.FirstOrDefault().Value}" : "";
                var merchant = TextManager.Get($"storename.{store.Key}").Value;
                if(merchant.Length == 0)
                {
                    merchant = TextManager.Get($"{store.Key}").Value;
                    if(merchant.Length == 0)
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
            foreach (var tuple in allTexts)
            {
                String id = string.Concat(tuple.Item1, string.Concat(Enumerable.Repeat(" ", (bigger - tuple.Item1.Length))));
                RichString text = $"{id}: ‖color:{Color.Gold}‖{tuple.Item2}‖color:end‖";
                new GUITextBlock(new RectTransform(new Vector2(1f, 0.1f), pricePerMerchantFramePriceValuesVerticalSplit.Content.rectTransform), RichString.Rich(text))
                {
                    CanBeFocused = false,
                    Font = GUIStyle.MonospacedFont,
                    TextSize = new Vector2(3f, 3f)
                };
            }
            
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
            var deconsFrame = new GUILayoutGroup(new RectTransform(new Vector2(1f, 1f), topFrame.RectTransform, anchor:Anchor.Center), childAnchor: Anchor.Center);
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
                        new GUIImage(new RectTransform(new Vector2(0.3f, 0.5f), arrowFrame.rectTransform,anchor:Anchor.Center), "GUIButtonHorizontalArrow", scaleToFit:false);
                        var boxItemIconCenter = new GUILayoutGroup(new RectTransform(new Vector2(0.9f, 0.9f), boxItemIcon.rectTransform,anchor:Anchor.Center), childAnchor: Anchor.Center);
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
            //GUIListBox list = new GUIListBox(new RectTransform(new Vector2(0.95f, 0.95f), innerFrame.rectTransform, Anchor.Center), false);
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
                if(itemicon != null)
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
            var noRecipeNeededFrame = new GUIListBox(new RectTransform(new Vector2(0.9f, 0.9f), paddednrnf.Content.rectTransform, anchor:Anchor.Center), style: null);
            AddRecipesToList((List<FabricationRecipe>)l.Where(static r => { return r.RequiresRecipe == false; }).ToList(), noRecipeNeededFrame.Content.rectTransform);
            new GUIFrame(new RectTransform(new Vector2(0.9f, 0.01f), targetRect), style: "HorizontalLine");
            if (lrecipeNeeded.Count <= 0) return;
            new GUITextBlock(new RectTransform(new Vector2(1f, 0.05f), targetRect), "Require a Recipe",textAlignment: Alignment.Center)
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
        public static void AddRecipesToList(List<FabricationRecipe> l, RectTransform targetRect )
        {
            if (l == null || targetRect == null) return;

            foreach (var recipe in l)
            {
                if (recipe == null || !(recipe.RequiredItems.Count() > 0))
                {
                    continue;
                }

                // symbole '‖'
                GUIListBox recipeLine = new GUIListBox(new RectTransform(new Vector2(1f, 0.2f), targetRect), isHorizontal: true, style:null);
                GUIListBox lrecipe = new GUIListBox(new RectTransform(new Vector2(0.5f, 1f), recipeLine.Content.rectTransform), isHorizontal: true, style: null);
                GUIListBox rrecipe = new GUIListBox(new RectTransform(new Vector2(0.5f, 1f), recipeLine.Content.rectTransform), isHorizontal: true, style: null);
                foreach (FabricationRecipe.RequiredItem ip in recipe.RequiredItems)
                {
                    ItemPrefab? curr_item_draw = null;
                    bool needToBeSwap = false;
                    float iconAlpha = 1f ;
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
                        var itemIconframe = new GUIFrame(new RectTransform(new Vector2(0.2f, 1f), lrecipe.Content.rectTransform), style:null);
                        GUILayoutGroup itemDeconsIconBox = new GUILayoutGroup(new RectTransform(new Vector2(1f,1f), itemIconframe.rectTransform, anchor:Anchor.Center),childAnchor:Anchor.Center);


                        // ITEM CONDITION CHECKING
                        // drawing after to not be under the item.
                        String conditionTooltip = getConditionTooltip(ip);
                        bool needCondition = conditionTooltip.Length > 0;
                        
                        RichString tooltip = conditionTooltip+ curr_item_draw.GetTooltip(Character.controlled);
                        var img = new GUIImage(new RectTransform(new Vector2(1f, 1.0f), itemDeconsIconBox.rectTransform), itemIcon, scaleToFit: true)
                        {
                            Color = curr_item_draw.InventoryIconColor,
                            toolTip = RichString.Rich(tooltip),
                        };
                        if(needToBeSwap)
                            item_swap_over_time.Add((img,ip));
                        // WRITE CONDITION OVER ITEM
                        if(needCondition)
                        {
                            var cmin = getItemConditionColor(ip.MinCondition);
                            var cmax = getItemConditionColor(ip.MaxCondition);
                            var barFrame = new GUIFrame(new RectTransform(new Vector2(0.8f, 0.1f), itemIconframe.rectTransform,anchor:Anchor.BottomCenter),style:null);
                            new GUIProgressBar(new RectTransform(new Vector2(1f, 1f), barFrame.rectTransform, anchor:Anchor.BottomCenter), (ip.MinCondition > ip.MaxCondition)?ip.MinCondition:ip.MaxCondition, color:cmin)
                            {
                                isHorizontal = true,
                                CanBeFocused = false,
                            };
                            //new GUITextBlock(new RectTransform(new Vector2(0f, 1f), lrecipe.Content.rectTransform), $"{ip.MinCondition * 100}", font: GUIStyle.SmallFont, textColor: cmin, textAlignment: Alignment.CenterRight);
                            //new GUITextBlock(new RectTransform(new Vector2(0f, 1f), lrecipe.Content.rectTransform), $"{ip.MaxCondition * 100}", font: GUIStyle.SmallFont, textColor: cmax, textAlignment: Alignment.TopRight);
                        }
                        new GUITextBlock(new RectTransform(new Vector2(1f, 0.2f), itemIconframe.rectTransform, anchor: Anchor.BottomRight), $"x{ip.Amount}", textColor: GUIStyle.TextColorBright.Value)
                        {
                            CanBeFocused=false,
                        };
                        imageToItem.Add((img, curr_item_draw));
                    }
                }
                var outputIcon = recipe.TargetItem.InventoryIcon ?? recipe.TargetItem.Sprite;
                if (outputIcon != null)
                {
                    new GUITextBlock(new RectTransform(new Vector2(0.8f, 1f), rrecipe.Content.rectTransform), $"", textAlignment: Alignment.BottomRight)
                    {
                        isSelected=false,
                        CanBeFocused=false,
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

        static public List<(GUIImage,RequiredItem)> item_swap_over_time = new();

        static int _refreshIconIndex = 0;
        static public void refreshMultipleRequired(Object source, ElapsedEventArgs e)
        {
            _refreshIconIndex++;
            if (item_swap_over_time.Count > 0)
            {
                foreach(var tuple in item_swap_over_time)
                {
                    int iconIndex = (int)(_refreshIconIndex % tuple.Item2.ItemPrefabs.Count());

                    var requiredItemToDisplay = tuple.Item2.ItemPrefabs.Skip(iconIndex).FirstOrDefault();
                    var img = requiredItemToDisplay?.InventoryIcon ?? requiredItemToDisplay?.Sprite;
                    if(requiredItemToDisplay != null && img != null)
                    {
                        tuple.Item1.UserData = requiredItemToDisplay;
                        tuple.Item1.toolTip = RichString.Rich(getConditionTooltip(tuple.Item2)+ requiredItemToDisplay.GetTooltip(Character.controlled));
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
            for (var i =0; i < allGUIComponents.Count ; i++){
                GUIComponent comp = allGUIComponents.ElementAt(i);
                if(comp != null) { 
                    comp?.Parent?.RemoveChild(comp);
                    comp?.ClearChildren();
                    //comp = null;
                } 
            }
            allGUIComponents.Clear();
            imageToItem.Clear();
            // topFrame?.Parent.RemoveChild(topFrame);
            // topFrame = null;
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
            foreach(var comp in imageToItem)
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

    class InventoryPatch
    {
        static public Barotrauma.ItemPrefab? LastOver = null;
        static bool searchDone = true;
        static DateTime timeCallSearch = DateTime.Now;
        /// <summary>
        /// Check after the keyboard input if mouse still within the ?hitbox? of the item Hovered.
        /// </summary>
        public static void checkInput()
        {
            //string text = "";
            //foreach (var a in GUIStyle.ComponentStyles)
            //{
            //    text += $"{a.Name} - ";
            //}
            //DebugConsole.NewMessage(text);
            bool mouseOn = target_interactRect.Contains(PlayerInput.MousePosition);
            if (mouseOn)
            {
                swapSave.Clear();
                swapped = false;
                LastOver = itemMouseHovering;
                searchForItem();
            }
        }

        public static List<Barotrauma.ItemPrefab> swapSave = new();
        public static bool swapped = false;

        /// <summary>
        /// Swap the current item for another one. Allow a continuous search. 
        /// </summary>
        /// <param name="i"> The new item to focus for search </param>
        public static void swapSubItem(Barotrauma.ItemPrefab i)
        {
            if(LastOver != null)
            {
                if (LastOver != i)
                    swapSave.Add(LastOver);
                else
                    return;
            }
            swapped = true;
            LastOver = i;
            searchDone = false;
            GUI.Clear();
            searchFabricatorRecipe();
            searchDone = true;
            GUI.Open(CharacterHUD.HUDFrame.rectTransform);
        }
        /// <summary>
        /// Unswap the stack of item searched. Allow to go back until we reach the first item. 
        /// </summary>
        public static void unswapSubItem()
        {
            if (swapped && swapSave.Count > 0)
                LastOver = swapSave.Last();
            if(LastOver != null && swapSave.Count > 0)
                swapSave.RemoveAt(swapSave.Count - 1);
            if (swapSave.Count == 0)
                swapped = false;
            if (LastOver == null)
                GUI.Close();
            GUI.Clear();
            searchFabricatorRecipe();
            searchDone = true;
            GUI.Open(CharacterHUD.HUDFrame.rectTransform);
        }
        /// <summary>
        /// Main function to search usage. Will prevent too frequent call {200ms}
        /// If a search is still in effect prevent any other call. => searchDone [flag
        /// </summary>
        public static void searchForItem()
        {
            TimeSpan elapsed = DateTime.Now - timeCallSearch;
            if (!(elapsed.TotalMilliseconds >= 200)) return;
            timeCallSearch = DateTime.Now;
            searchDone = false;
            if (LastOver != null)
            {
                DebugConsole.NewMessage("Starts looking for recipe");
                searchFabricatorRecipe();
                GUI.Open(CharacterHUD.HUDFrame.rectTransform);
            }
            searchDone = true;
        }

        /// <summary>
        /// Write the fabricator recipe found in the Debug console. First use of the mod, now only for debug.
        /// </summary>
        public static void printFabricatorProducer()
        {
            if(Producers != null && Producers.Count > 0)
            foreach(FabricationRecipe recipe in Producers)
            {
                String recipeString = "";
                foreach (var reqItem in recipe.RequiredItems)
                {
                    recipeString += $"{reqItem.Amount} of :\n--";
                    foreach (var subItem in reqItem.ItemPrefabs)
                    {
                        recipeString += $"{subItem.Name},";
                    }
                    recipeString += "\n";
                }
                if (recipeString.Length > 0)
                {
                    DebugConsole.NewMessage($"____________________\n", Color.HotPink);
                    DebugConsole.NewMessage(recipeString, Color.Aqua);
                    DebugConsole.NewMessage($" Will produce :  | {recipe.Amount} | of {recipe.TargetItem.Name}", Color.Gold);
                    DebugConsole.NewMessage($"____________________\n", Color.HotPink);

                }
            }
        }
        /// <summary>
        /// Same as <see cref="printFabricatorProducer"/>
        /// </summary>
        public static void printFabricatorUsages()
        {
            DebugConsole.NewMessage("Usages : \n");
            if (Usages != null && Usages.Count > 0)
            {
                DebugConsole.NewMessage($"{Usages.Count}\n");
                foreach (FabricationRecipe recipe in Usages)
                {
                    String recipeString = "";
                    foreach (var reqItem in recipe.RequiredItems)
                    {
                        recipeString += $"{reqItem.Amount} of :\n--";
                        foreach (var subItem in reqItem.ItemPrefabs)
                        {
                            recipeString += $"{subItem.Name},";
                        }
                        recipeString += "\n";
                    }
                    if (recipeString.Length > 0)
                    {
                        DebugConsole.NewMessage($"____________________\n", Color.HotPink);
                        DebugConsole.NewMessage(recipeString, Color.Aqua);
                        DebugConsole.NewMessage($" Will produce :  | {recipe.Amount} | of {recipe.TargetItem.Name}", Color.Gold);
                        DebugConsole.NewMessage($"____________________\n", Color.HotPink);
                    }
                }
            }
        }

        public static List<FabricationRecipe> Producers = new List<FabricationRecipe>();
        public static List<FabricationRecipe> Usages = new List<FabricationRecipe>();
        public static List<DeconstructItem> DeconstructItems = new List<DeconstructItem>();
        public static List<ItemPrefab> ProduceWhenDeconstruct= new List<ItemPrefab>();
        

        public static List<FabricationRecipe> allRecipes = new List<FabricationRecipe>();
        /// <summary>
        /// Will iterate through all the fabricator recipes and deconstructor to search for any link with the item
        /// that is LastOver.
        /// </summary>
        public static void searchFabricatorRecipe()
        {
            DebugConsole.NewMessage($"{allRecipes.Count} recipes to search");
            if (LastOver != null)
            {
                Producers.Clear();
                Usages.Clear();
                DeconstructItems.Clear();
                ProduceWhenDeconstruct.Clear();
                if (LastOver.AllowDeconstruct)
                    DeconstructItems = LastOver.DeconstructItems.ToList();
                foreach (var item in ItemPrefab.Prefabs) {
                    if (item.DeconstructItems != null)
                    {
                        if (item.DeconstructItems.FindIndex(static p => p.ItemIdentifier == LastOver.Identifier) > 0)
                            ProduceWhenDeconstruct.Add(item);
                    }
                }
                //foreach (var kvp in LastOver.Prefab.FabricationRecipes)
                foreach(FabricationRecipe recipe in allRecipes)
                {
                    ItemPrefab i = recipe.TargetItem;
                    if (LastOver.Name == i.Name)
                    {
                        Producers.Add(recipe);
                    }
                    else
                    {
                        foreach (var rqitem in recipe.RequiredItems)
                        {
                            bool toAdd = false;
                            bool dfItem = rqitem.DefaultItem == LastOver.Identifier;
                            bool uiid = rqitem.UintIdentifier == LastOver.UintIdentifier;
                            //bool match = rqitem.MatchesItem(LastOver);
                            if (dfItem || uiid)
                                toAdd = true;
                            if (toAdd)
                            {
                                Usages.Add(recipe);
                                break;
                            }
                        }
                    }
                }
                //printFabricatorProducer();
                //printFabricatorUsages();
            }
        }

        static Rectangle target_interactRect;

        static public Barotrauma.ItemPrefab? itemMouseHovering;

        /// <summary>
        /// Core function of the mod, (weird postfix with Harmony, i had nothing better).
        /// This function is called when the user interact with an inventory, we get each slot
        /// we check and if one slot has the mouse over then it will be true and we save the item.
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="slot"></param>
        /// <param name="slotIndex"></param>
        /// <param name="item"></param>
        /// <param name="isSubSlot"></param>
        public static void UpdateSlotPostfix(Inventory __instance, VisualSlot slot, int slotIndex, Barotrauma.Item item, bool isSubSlot)
        {
            // Logique à exécuter après l'appel de la méthode originale
            //DebugConsole.NewMessage($"UpdateSlot called with slotIndex: {slotIndex}, isSubSlot: {isSubSlot}", Color.Gold);

            // Répliquer la logique de la ligne spécifique
            Rectangle interactRect = slot.InteractRect; // Assurez-vous que c'est la bonne référence
            interactRect.Location += slot.DrawOffset.ToPoint();
            bool locked = __instance.Locked; // Assurez-vous que c'est la bonne référence
            bool mouseOnGUI = false; // Assurez-vous que c'est la bonne référence
            bool isMouseOnInventory = Inventory.IsMouseOnInventory; // Assurez-vous que c'est la bonne référence

            bool mouseOn = interactRect.Contains(PlayerInput.MousePosition) &&
                           !locked &&
                           !mouseOnGUI &&
                           !slot.Disabled &&
                           isMouseOnInventory;

            // Utilisez 'mouseOn' comme nécessaire dans votre logique
            if (mouseOn && item != null)
            {
                if (searchDone)
                {
                    itemMouseHovering = item.Prefab;
                    target_interactRect = interactRect;
                }
            }
        }
    }





}
