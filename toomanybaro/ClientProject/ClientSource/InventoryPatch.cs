using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Barotrauma;
using Barotrauma.Extensions;
using Microsoft.Xna.Framework;

namespace tooManyBaro.ClientSource
{
    class InventoryPatch
    {

        static public List<Barotrauma.ItemPrefab> resultOfSearch = new List<ItemPrefab>();
        static public Barotrauma.ItemPrefab? LastOver = null;
        static bool searchDone = true;
        static DateTime timeCallSearch = DateTime.Now;
        /// <summary>
        /// Check after the keyboard input if mouse still within the ?hitbox? of the item Hovered.
        /// </summary>
        public static void checkInput()
        {
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
            if (LastOver != null)
            {
                if (LastOver != i)
                    swapSave.Add(LastOver);
                else
                    return;
            }
            swapped = true;
            LastOver = i;
            searchDone = false;
            tooManyBaro.ClientSource.GUI.Clear();
            searchFabricatorRecipe();
            searchDone = true;
            tooManyBaro.ClientSource.GUI.Open(CharacterHUD.HUDFrame.rectTransform);
        }
        /// <summary>
        /// Unswap the stack of item searched. Allow to go back until we reach the first item. 
        /// </summary>
        public static void unswapSubItem()
        {
            if (swapped && swapSave.Count > 0)
                LastOver = swapSave.Last();
            if (LastOver != null && swapSave.Count > 0)
                swapSave.RemoveAt(swapSave.Count - 1);
            if (swapSave.Count == 0)
                swapped = false;
            if (LastOver == null)
                tooManyBaro.ClientSource.GUI.Close();
            tooManyBaro.ClientSource.GUI.Clear();
            searchFabricatorRecipe();
            searchDone = true;
            tooManyBaro.ClientSource.GUI.Open(CharacterHUD.HUDFrame.rectTransform);
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
                tooManyBaro.ClientSource.GUI.Open(CharacterHUD.HUDFrame.rectTransform);
            }
            searchDone = true;
        }

        /// <summary>
        /// Write the fabricator recipe found in the Debug console. First use of the mod, now only for debug.
        /// </summary>
        public static void printFabricatorProducer()
        {
            if (Producers != null && Producers.Count > 0)
                foreach (FabricationRecipe recipe in Producers)
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
        public static List<ItemPrefab> ProduceWhenDeconstruct = new List<ItemPrefab>();


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
                foreach (var item in ItemPrefab.Prefabs)
                {
                    if (item.DeconstructItems != null)
                    {
                        if (item.DeconstructItems.FindIndex(static p => p.ItemIdentifier == LastOver.Identifier) > 0)
                            ProduceWhenDeconstruct.Add(item);
                    }
                }
                //foreach (var kvp in LastOver.Prefab.FabricationRecipes)
                foreach (FabricationRecipe recipe in allRecipes)
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

        /// <summary>
        /// Looks for item that contain the string passed in argument.
        /// </summary>
        /// <param name="name">regex element to search for</param>
        static public void searchItemWithString(string name)
        {
            resultOfSearch.Clear();
            foreach (var item in ItemPrefab.Prefabs)
            {
                if(item.Name.Contains(name))
                    resultOfSearch.Add(item);

                    //if (Regex.IsMatch(item.name.Value, name))
            }
            return;
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
