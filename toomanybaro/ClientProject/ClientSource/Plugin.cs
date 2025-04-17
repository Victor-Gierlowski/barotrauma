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
using Barotrauma.Items.Components;
using EventInput;
using HarmonyLib;
using JetBrains.Annotations;
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
            tooManyBaro.ClientSource.KeyBoardInput myInputs = new tooManyBaro.ClientSource.KeyBoardInput();
            Barotrauma.DebugConsole.NewMessage("created a harmony",Color.Gold);
            // Utiliser AccessTools pour obtenir la méthode privée
            var originalMethod = AccessTools.Method(typeof(Inventory), "UpdateSlot");
            if (originalMethod != null)
            {
                harmony.Patch(
                    original: originalMethod,
                    postfix: new HarmonyMethod(typeof(tooManyBaro.ClientSource.InventoryPatch).GetMethod("UpdateSlotPostfix"))
                );
            }
            else
            {
                DebugConsole.NewMessage("Protected method UpdateSlot not found", Color.Red);
            }
            Barotrauma.DebugConsole.NewMessage("Loaded MouseInventory", Color.Gold);

            Barotrauma.DebugConsole.commands.Add(new DebugConsole.Command("tmb_unsafe", "toggle the unsafe mod for the ui opener, MAY CAUSE CRASH, FOR DEBUG ONLY TO LOCATE SPECIFICS PROBLEMS",(args) =>
            {
                tooManyBaro.ClientSource.GUI.UNSAFE = !tooManyBaro.ClientSource.GUI.UNSAFE;
                if (tooManyBaro.ClientSource.GUI.UNSAFE)
                    DebugConsole.NewMessage("CURRENTLY IN UNSAFE MOD PROCEED WITH CAUTION", color: Color.Red);
                else
                    DebugConsole.NewMessage("Back in SAFE mod", color: Color.DarkGreen);
            },isCheat:false));

            harmony.Patch(
                original: typeof(PlayerInput).GetMethod("Update"),
                postfix: new HarmonyMethod(typeof(tooManyBaro.ClientSource.KeyBoardInput).GetMethod("onUpdateKeys"))
                );
            Barotrauma.DebugConsole.NewMessage("Loaded updateKeys Settings Menu", Color.Gold);


            Barotrauma.DebugConsole.NewMessage("Loading Custom Options");
            try
            {
                tooManyBaro.ClientSource.Options.loadOptions();
            }
            catch (Exception ex)
            {
                DebugConsole.NewMessage($"TooManyBaro failed to load options : {ex.Message} {ex.StackTrace}",color:Color.OrangeRed);
            }

        }
        public void OnLoadCompleted() {
            foreach (ItemPrefab iprefab in ItemPrefab.Prefabs)
            {
                if (iprefab.HasSubCategory("subweapons")){
                    if (iprefab.Tags.Contains(Tags.TurretAmmoSource)){
                        ItemPrefab? targetTurret = ItemPrefab.Prefabs.Find(el => el.Identifier == iprefab.allowedLinks.FirstOrDefault());
                        if (targetTurret == null) continue;
                        DebugConsole.NewMessage($"{iprefab.Identifier.ToString()} -<- subweapon ammo source {iprefab.GetType().ToString()}", color: Color.Cyan);
                        var config = iprefab.ConfigElement;
                        foreach (var item in config.Elements())
                        {
                            if(item.Name.ToString().ToLowerInvariant() == "itemcontainer")
                            {
                                foreach (var item1 in item.Elements())
                                {
                                    if(item1.Name.ToString().ToLowerInvariant() == "containable")
                                    {
                                        foreach (var item2 in item1.Attributes())
                                        {
                                            var ammoTags= item2.Value.ToIdentifier();
                                            tooManyBaro.ClientSource.InventoryPatch.subWeaponsMatchingTags.Add((targetTurret,ammoTags));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                if (iprefab.CanBeBought || iprefab.CanBeSold || iprefab.FabricationRecipes.Count != 0 || iprefab.AllowDeconstruct)
                    if (!tooManyBaro.ClientSource.InventoryPatch.SearchListPrefabs.Contains(iprefab))
                        tooManyBaro.ClientSource.InventoryPatch.SearchListPrefabs.Add(iprefab);
                foreach (FabricationRecipe recipe in iprefab.FabricationRecipes.Values)
                {
                    tooManyBaro.ClientSource.InventoryPatch.allRecipes.Add(recipe);
                    if(recipe.RequiredItems.Any())
                    foreach(var ingredient in recipe.RequiredItems)
                    {
                            foreach(var subingredient in ingredient.ItemPrefabs)
                            {
                                if (subingredient != null)
                                    if (!tooManyBaro.ClientSource.InventoryPatch.SearchListPrefabs.Contains(subingredient))
                                        tooManyBaro.ClientSource.InventoryPatch.SearchListPrefabs.Add(subingredient);
                            }
                    }
                }
            }
        }
        public void PreInitPatching() { }
        public void Dispose()
        {
            harmony.UnpatchSelf();
            harmony = null;
        }
    }
}
