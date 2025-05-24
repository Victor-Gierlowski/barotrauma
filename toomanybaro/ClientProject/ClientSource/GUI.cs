using System.Timers;
using Barotrauma;
using Barotrauma.Extensions;
using Microsoft.Xna.Framework;
using toomanybaro.ClientSource;
using static Barotrauma.FabricationRecipe;

namespace tooManyBaro.ClientSource
{

    enum GUIToomManyBaro
    {
        recipes,
        deconstructs,
        infos,
        options,
        search
    }

    class GUI
    {
        public static bool isOpen = false;
        public static GUIToomManyBaro menuopen = GUIToomManyBaro.recipes;

        public static GUILayoutGroup? frame;
        public static GUILayoutGroup? innerFrame;
        public static GUILayoutGroup? mainFrame;
        public static GUIFrame? topFrame;

        public static List<GUIComponent> allGUIComponents = new();
        public static List<(GUIImage, ItemPrefab)> imageToItem = new();
        static public List<(GUIImage, RequiredItem)> item_swap_over_time = new();

        static int _refreshIconIndex = 0;

        public static System.Timers.Timer? _Refresh_multiple_item;

        public static bool UNSAFE = false;
        /// <summary>
        /// Function to open the UI. Call the corresponding tab and check if something goes wrong. Catch errors and prevent any crash.
        /// </summary>
        /// <param name="rectTransform">main target to draw and inherit all the ui. (CharacterHUD.HUDFrame.rectTransform)</param>
        public static void Open(RectTransform rectTransform)
        {
            if (UNSAFE)
                __unsecure_open(rectTransform);
            else
                try
                {
                    __unsecure_open(rectTransform);
                }
                catch (Exception ex)
                {
                    DebugConsole.NewMessage($"TooManyBaro UI open crash handler : {ex.Message}", color: Color.DarkRed);
                    DebugConsole.NewMessage($"TooManyBaro UI open crash handler : {ex.StackTrace}", color: Color.DarkRed);
                    string itemName = "[null] ? ";
                    if (InventoryPatch.LastOver != null)
                    {
                        itemName = InventoryPatch.LastOver.ToString();
                    }
                    DebugConsole.NewMessage($"TooManyBaro UI open crash handler : item was {itemName}", color: Color.DarkRed);
                    DebugConsole.NewMessage($"TooManyBaro UI open crash handler : Open is cancel. (item chosen may have caused this, choose another.)", color: Color.DarkRed);
                    Close();
                    isOpen = false;
                    menuopen = GUIToomManyBaro.recipes;
                }
        }

        /// <summary>
        /// opening and setings values of GUIComponents. May crash if something goes wrong in subtab.
        /// </summary>
        /// <param name="rectTransform"></param>
        private static void __unsecure_open(RectTransform rectTransform)
        {
            if (_Refresh_multiple_item == null)
            {
                _Refresh_multiple_item = new System.Timers.Timer(Options.refresh_time);
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
                GUIrecipes.drawRecipes(wholeframe,topFrame);
            else if (menuopen == GUIToomManyBaro.deconstructs)
                GUIdeconstruct.drawDeconstruct(topFrame);
            else if (menuopen == GUIToomManyBaro.infos)
            {
                GUIinfos.drawInfos(topFrame);
            }
            else if (menuopen == GUIToomManyBaro.options)
                GUIoptions.drawOptions(topFrame);
            else if (menuopen == GUIToomManyBaro.search)
                GUIsearch.drawSearch(wholeframe);
            else
            {
                GUIrecipes.drawRecipes(wholeframe,topFrame);
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
            new GUIButton(new RectTransform(new Vector2(1f, 0.1f), rightSideButtonList.Content.rectTransform)
            {
                //RelativeOffset = new Vector2(0.05f,0f)
            }, "Search")
            {
                OnClicked = (GUIButton button, object obj) =>
                {
                    menuopen = GUIToomManyBaro.search;
                    Open(CharacterHUD.HUDFrame.rectTransform);
                    return true;
                }
            };
            new GUIButton(new RectTransform(new Vector2(1f, 0.1f), rightSideButtonList.Content.rectTransform)
            {
                RelativeOffset = new Vector2(0.0f,0.1f)
            }, "Mod options")
            {
                OnClicked = (GUIButton button, object obj) =>
                {
                    menuopen = GUIToomManyBaro.options;
                    Open(CharacterHUD.HUDFrame.rectTransform);
                    return true;
                }
            };
            _Refresh_multiple_item.Start();
        }
        
        //  color symbol ‖
        /// <summary>
        /// get a bool value and prefix text, will return a RichString that will contain a colored result of the boolean and the prefix.
        /// </summary>
        /// <param name="B"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public static RichString addBoolRichString(bool B, string text = "")
        {
            var cfalse = GUIStyle.ColorReputationVeryLow.Value;
            var ctrue = GUIStyle.ColorReputationVeryHigh.Value;
            var c = B ? ctrue : cfalse;
            var t = B ? "true" : "false";
            return $"{text}‖color:{c}‖{t}‖color:end‖";
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
                var ip = ItemPrefab.Find(item.ToString(), item.ItemIdentifier);
                if(ip == null)
                {
                    DebugConsole.NewMessage($"tooManyBaro: ADTL item prefab not found: {item.ToString()} - {item.ItemIdentifier}; mod related ? itemIdentifier / name are not correct.",Color.DarkRed);
                    continue;
                }
                //var ip = (ItemPrefab)ItemPrefab.FindByIdentifier(item.ItemIdentifier);
                var itemicon = ip.InventoryIcon ?? ip.Sprite;
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

        /// <summary>
        /// Return the color corresponding to the condition given.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        static public Color getItemConditionColor(float value)
        {
            return ((value > .70) ? GUIStyle.ColorReputationHigh : (value > .30) ? GUIStyle.ColorReputationNeutral : GUIStyle.ColorReputationVeryLow);
        }

        /// <summary>
        /// get an item and add the condition required within the tooltip.
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
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

        /// <summary>
        /// update the items that need to swap for subingredient. Data to swap must be contain in the userdata.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        static public void refreshMultipleRequired(Object? source, ElapsedEventArgs e)
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

        /// <summary>
        /// Close the UI.
        /// </summary>
        public static void Close()
        {
            Clear();
            isOpen = false;
            InventoryPatch.swapSave.Clear();
            if (Options.reopen_recipes_after_close) menuopen = GUIToomManyBaro.recipes;
            //if (menuopen == GUIToomManyBaro.options)
            //    menuopen = GUIToomManyBaro.recipes;
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
            clearImgToItem();
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
            {
                if (menuopen == GUIToomManyBaro.search)
                    menuopen = GUIToomManyBaro.infos;
                InventoryPatch.swapSubItem(toswapto);
            }
        }

        /// <summary>
        /// empty all the references of img and rect for leftclick.
        /// </summary>
        internal static void clearImgToItem()
        {
            //if(imageToItem.Any())
            //foreach(var T in imageToItem)
            //    {
                    
            //    }
            imageToItem.Clear();
        }
    }

}
