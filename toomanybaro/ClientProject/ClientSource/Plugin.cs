using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Barotrauma;
using Barotrauma.Extensions;
using EventInput;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Mono.Cecil;
using static Barotrauma.PetBehavior.ItemProduction;



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
                original: typeof(SettingsMenu).GetMethod("Create"),
                postfix: new HarmonyMethod(typeof(SettingsMenuPatch).GetMethod("Create"))
                );
            Barotrauma.DebugConsole.NewMessage("Loaded Create Settings Menu", Color.Gold);

            harmony.Patch(
                original: typeof(PlayerInput).GetMethod("Update"),
                postfix: new HarmonyMethod(typeof(toomanybaroKeyBoardInput).GetMethod("onUpdateKeys"))
                );
            Barotrauma.DebugConsole.NewMessage("Loaded updateKeys Settings Menu", Color.Gold);

        }
        public void OnLoadCompleted() {
            foreach (ItemPrefab iprefab in ItemPrefab.Prefabs)
            {
                foreach(FabricationRecipe recipe in iprefab.FabricationRecipes.Values)
                    InventoryPatch.allRecipes.Add(recipe);
            }
            DebugConsole.NewMessage($"Found {InventoryPatch.allRecipes.Count} recipes ! ");
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
        public static void onUpdateKeys(double deltaTime)
        {
            Keys[] kPressed = PlayerInput.keyboardState.GetPressedKeys();
            foreach(Keys key in kPressed)
            {
                if (key == Keys.O)
                    InventoryPatch.checkInput();
            }
        }
    }

    class SettingsMenuPatch
    {
        public static void Create(RectTransform mainParent)
        {
            DebugConsole.NewMessage("AAAAAA EN?FIN ?? ?", Color.Gold);
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
            topFrame = new GUIFrame(new RectTransform(new Vector2(1.0f, 0.65f), mainFrame.RectTransform), style: "InnerFrameDark");
            // PRODUCERS SIDE
            itemListFrame = new GUILayoutGroup(new RectTransform(new Vector2(0.5f, 1.0f), topFrame.RectTransform), childAnchor: Anchor.Center);
            var itemList = new GUIListBox(new RectTransform(new Vector2(1f, 0.9f), itemListFrame.RectTransform), style: null);
            paddedItemFrame = new GUILayoutGroup(new RectTransform(new Vector2(0.9f, 0.9f), itemListFrame.RectTransform));
            new GUITextBlock(new RectTransform(new Vector2(1f, 0.1f), itemList.Content.RectTransform), "List of Recipe as Target", textAlignment: Alignment.Center);

            // SEPARATOR FOR LEFT AND RIGHT
            new GUIFrame(new RectTransform(new Vector2(0.01f, 0.9f), topFrame.RectTransform, Anchor.Center), style: "VerticalLine");

            // USAGES SIDE

            usageFrame = new GUILayoutGroup(new RectTransform(new Vector2(0.5f, 1f), topFrame.RectTransform, Anchor.TopRight), childAnchor: Anchor.Center);
            var itemListUsage = new GUIListBox(new RectTransform(new Vector2(1f, 0.9f), usageFrame.RectTransform), style: null);
            paddedUsageFrame = new GUILayoutGroup(new RectTransform(new Vector2(0.95f, 0.9f), usageFrame.RectTransform));
            new GUITextBlock(new RectTransform(new Vector2(1f, 0.1f), itemListUsage.Content.RectTransform), "List Of Recipe as Ingredient", textAlignment: Alignment.Center);

            //GUIListBox list = new GUIListBox(new RectTransform(new Vector2(0.95f, 0.95f), innerFrame.rectTransform, Anchor.Center), false);
            AddRecipesToList(InventoryPatch.Producers, itemList.Content.rectTransform);
            AddRecipesToList(InventoryPatch.Usages, itemListUsage.Content.rectTransform);

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

        public static void AddRecipesToList(List<FabricationRecipe> l, RectTransform targetRect )
        {
            foreach (var recipe in l)
            {
                GUIListBox lrecipe = new GUIListBox(new RectTransform(new Vector2(1.0f, 0.1f), targetRect), isHorizontal: true);
                foreach (FabricationRecipe.RequiredItem ip in recipe.RequiredItems)
                {
                    var itemIcon = ip.ItemPrefabs.First().InventoryIcon ?? ip.ItemPrefabs.First().Sprite;
                    if (itemIcon != null)
                    {
                        new GUITextBlock(new RectTransform(new Vector2(0f, 1f), lrecipe.Content.rectTransform), $"x{ip.Amount}", textAlignment: Alignment.BottomRight);
                        new GUIImage(new RectTransform(new Vector2(0.1f, 1.0f), lrecipe.Content.rectTransform), itemIcon, scaleToFit: true)
                        {
                            Color = ip.ItemPrefabs.First().InventoryIconColor,
                            toolTip = RichString.Rich(ip.ItemPrefabs.First().Description)
                        };
                    }
                }
                allGUIComponents.Add(lrecipe);
            }
        }

        public static void Close()
        {
            for(var i =0; i < allGUIComponents.Count ; i++){
                GUIComponent comp = allGUIComponents.ElementAt(i);
                if(comp != null) { 
                    comp?.Parent?.RemoveChild(comp);
                    comp?.ClearChildren();
                    //comp = null;
                } 
            }
            allGUIComponents.Clear();
            // topFrame?.Parent.RemoveChild(topFrame);
            // topFrame = null;
        }
    }

    class InventoryPatch
    {
        static Barotrauma.Item? LastOver = null;
        static bool searchDone = true;
        static DateTime timeCallSearch = DateTime.Now;
        /**
         * Check after the keyboard input if mouse still within the ?hitbox? of the item Hovered.
         */
        public static void checkInput()
        {
            bool mouseOn = target_interactRect.Contains(PlayerInput.MousePosition);
            if (mouseOn)
                searchForItem();
        }

        /**
         * Main function to search usage. Will prevent too frequent call {200ms}
         * If a search is still in effect prevent any other call. => searchDone [flag]
         */
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
        /**
         * Go through fabricator recipe that used or produce the item
         */
        public static List<FabricationRecipe> Producers = new List<FabricationRecipe>();
        public static List<FabricationRecipe> Usages = new List<FabricationRecipe>();

        public static List<FabricationRecipe> allRecipes = new List<FabricationRecipe>();

        public static void searchFabricatorRecipe()
        {
            DebugConsole.NewMessage($"{allRecipes.Count} recipes to search");
            if (LastOver != null)
            {
                Producers.Clear();
                Usages.Clear();
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
                            bool dfItem = rqitem.DefaultItem == LastOver.Prefab.Identifier;
                            bool uiid = rqitem.UintIdentifier == LastOver.Prefab.UintIdentifier;
                            bool match = rqitem.MatchesItem(LastOver);
                            if (dfItem || match || uiid)
                                toAdd = true;
                            if (toAdd)
                            {
                                Usages.Add(recipe);
                                break;
                            }
                        }
                    }
                }
                printFabricatorProducer();
                printFabricatorUsages();
            }
        }

        static Rectangle target_interactRect;
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
                    LastOver = item;
                    target_interactRect = interactRect;
                }
            }
        }
    }



}
