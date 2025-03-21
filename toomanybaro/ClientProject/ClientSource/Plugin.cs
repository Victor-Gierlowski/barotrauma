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


    class GUI
    {
        private static GUILayoutGroup? frame;
        private static GUILayoutGroup? innerFrame;
        private static GUILayoutGroup? mainFrame;
        private static GUIFrame? topFrame;
        private static GUILayoutGroup? itemListFrame;
        private static GUILayoutGroup? paddedItemFrame;
        private static GUILayoutGroup? usageFrame;
        private static GUILayoutGroup? paddedUsageFrame;

        private static List<GUIComponent> allGUIComponents = new();

        /// <summary>
        /// Function to open the UI. Create all the subUI and fill the frames etc..
        /// </summary>
        /// <param name="rectTransform">main target to draw and inherit all the ui. (CharacterHUD.HUDFrame.rectTransform)</param>
        public static void Open(RectTransform rectTransform)
        {
            Close();
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

            var wholeframe = new GUIFrame(new RectTransform(new Vector2(1.0f, 0.65f), mainFrame.RectTransform));
            topFrame = new GUIFrame(new RectTransform(new Vector2(1.0f, 0.9f), wholeframe.RectTransform), style: "InnerFrameDark");

            

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
            allGUIComponents.Add(wholeframe);
            allGUIComponents.Add(deconsframe);
            allGUIComponents.Add(deconsList);
            //GUIListBox list = new GUIListBox(new RectTransform(new Vector2(0.95f, 0.95f), innerFrame.rectTransform, Anchor.Center), false);
            AddRecipesToListFilter(InventoryPatch.Producers, itemList.Content.rectTransform, InventoryPatch.ProduceWhenDeconstruct);
            AddRecipesToListFilter(InventoryPatch.Usages, itemListUsage.Content.rectTransform);
            AddDeconstructToList(InventoryPatch.DeconstructItems, deconsList.Content.rectTransform);

            new GUIButton(new RectTransform(new Vector2(0.1f, 0.01f), innerFrame.RectTransform, Anchor.BottomCenter)
            {
                RelativeOffset = new Vector2(0f, 0.05f)
            }, "Close")
            {
                OnClicked = (GUIButton button, object obj) =>
                {
                    Close();

                    return true;
                }
            };
            allGUIComponents.Add(frame);
            allGUIComponents.Add(innerFrame);
            allGUIComponents.Add(mainFrame);
            allGUIComponents.Add(topFrame);
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
            GUIListBox itemlist = new GUIListBox(new RectTransform(new Vector2(1f, 0.5f), rectTransform), isHorizontal: true);
            foreach (var item in deconstructItems)
            {
                var ip = (ItemPrefab)ItemPrefab.FindByIdentifier(item.ItemIdentifier);
                var itemicon = ip.InventoryIcon;
                if(itemicon != null)
                {
                    RichString tooltip = ((ItemPrefab)ip).GetTooltip(Character.controlled);
                    GUIImage img = new GUIImage(new RectTransform(new Vector2(0.2f, 0.8f), itemlist.Content.rectTransform), itemicon, scaleToFit: true)
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

            if(producewhenDecons != null && producewhenDecons.Count > 0)
            {
                //var paddedProduceWhenDecons = new GUIListBox(new RectTransform(new Vector2(1f, 0.7f), targetRect), style: "ItemUI");
                //var ProduceWhenDeconsFrame = new GUIListBox(new RectTransform(new Vector2(0.9f, 0.9f), paddedProduceWhenDecons.Content.rectTransform, anchor: Anchor.Center), style: null);
                //foreach(var item in producewhenDecons)
                //    if (item.AllowDeconstruct)
                //    {
                //        var il = new GUIListBox(new RectTransform(new Vector2(1f, 0.1f), ProduceWhenDeconsFrame.Content.rectTransform), style: null, isHorizontal:true);
                //        var iIcon = new GUIListBox(new RectTransform(new Vector2(0.3f, 1f), ProduceWhenDeconsFrame.Content.rectTransform), style: null, isHorizontal:true);
                //        new GUIImage(new RectTransform(new Vector2(0.2f, 1f), iIcon.Content.rectTransform), item.InventoryIcon??item.sprite);
                //        var pItems= new GUIListBox(new RectTransform(new Vector2(0.3f, 1f), ProduceWhenDeconsFrame.Content.rectTransform), style: null,isHorizontal:true);
                //        AddDeconstructToList(item.DeconstructItems.ToList(), pItems.Content.rectTransform);
                //    }
                //new GUIFrame(new RectTransform(new Vector2(0.9f, 0.01f), targetRect), style: "HorizontalLine");
            }
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
                GUIListBox recipeLine = new GUIListBox(new RectTransform(new Vector2(1f, 0.2f), targetRect), isHorizontal: true, style:null);
                GUIListBox lrecipe = new GUIListBox(new RectTransform(new Vector2(0.5f, 1f), recipeLine.Content.rectTransform), isHorizontal: true, style: null);
                GUIListBox rrecipe = new GUIListBox(new RectTransform(new Vector2(0.5f, 1f), recipeLine.Content.rectTransform), isHorizontal: true, style: null);
                foreach (FabricationRecipe.RequiredItem ip in recipe.RequiredItems)
                {
                    var itemIcon = ip.ItemPrefabs.First().InventoryIcon ?? ip.ItemPrefabs.First().Sprite;
                    if (itemIcon != null)
                    {
                        RichString tooltip = ip.ItemPrefabs.First().GetTooltip(Character.controlled);
                        var img = new GUIImage(new RectTransform(new Vector2(0.2f, 1.0f), lrecipe.Content.rectTransform), itemIcon, scaleToFit: true)
                        {
                            Color = ip.ItemPrefabs.First().InventoryIconColor,
                            toolTip = RichString.Rich(tooltip),
                        };
                        // WRITE CONDITION OVER ITEM
                        if (ip.MinCondition != ip.MaxCondition)
                        {
                            var cmin = getItemConditionColor(ip.MinCondition);
                            var cmax = getItemConditionColor(ip.MaxCondition);
                            new GUITextBlock(new RectTransform(new Vector2(0f, 1f), lrecipe.Content.rectTransform), $"{ip.MinCondition * 100}", font: GUIStyle.SmallFont, textColor: cmin, textAlignment: Alignment.CenterRight);
                            new GUITextBlock(new RectTransform(new Vector2(0f, 1f), lrecipe.Content.rectTransform), $"{ip.MaxCondition * 100}", font: GUIStyle.SmallFont, textColor: cmax, textAlignment: Alignment.TopRight);
                        }
                        new GUITextBlock(new RectTransform(new Vector2(0f, 1f), lrecipe.Content.rectTransform), $"x{ip.Amount}", textAlignment: Alignment.BottomRight);
                        imageToItem.Add((img, ip.ItemPrefabs.First()));
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

        /// <summary>
        /// Close the UI, we also clear all the gui component. Why ? because all the exemples i could find did something
        /// similar and it was not working unless i do that. Breaking the order would also provoke null exception somewhere.
        /// </summary>
        public static void Close()
        {
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
                    toswapto = comp.Item2;
                    break;
                }
            }
            if (toswapto != null)
                InventoryPatch.swapSubItem(toswapto);
        }
    }

    class InventoryPatch
    {
        static Barotrauma.ItemPrefab? LastOver = null;
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
                searchForItem();
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
                swapSave.Add(LastOver);
            }
            swapped = true;
            LastOver = i;
            searchDone = false;
            GUI.Close();
            searchFabricatorRecipe();
            searchDone = true;
            GUI.Open(CharacterHUD.HUDFrame.rectTransform);
        }
        /// <summary>
        /// Unswap the stack of item searched. Allow to go back until we reach the first item. 
        /// </summary>
        public static void unswapSubItem()
        {
            if (swapped)
                LastOver = swapSave.Last();
            if(LastOver != null)
                swapSave.RemoveAt(swapSave.Count - 1);
            if (swapSave.Count == 0)
                swapped = false;
            GUI.Close();
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
                    LastOver = item.Prefab;
                    target_interactRect = interactRect;
                }
            }
        }
    }



}
