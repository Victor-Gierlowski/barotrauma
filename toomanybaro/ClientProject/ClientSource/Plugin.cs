using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Sockets;
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

    class InventoryPatch
    {
        static Item? LastOver = null;
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
        public static void UpdateSlotPostfix(Inventory __instance, VisualSlot slot, int slotIndex, Item item, bool isSubSlot)
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
                //if (PlayerInput.IsAltDown())
                //{
                //    if (searchDone)
                //    {
                //        DebugConsole.NewMessage("ASKED FOR INPUT WITH ALT");
                //        searchForItem();
                //    }
                //}
                //DebugConsole.NewMessage($"Mouse on slot: {mouseOn} i:{slotIndex} sub:{isSubSlot}", Color.Gold);
                //DebugConsole.NewMessage($"Item name: {item?.Name}",Color.Blue);
            }
        }
    }



}
