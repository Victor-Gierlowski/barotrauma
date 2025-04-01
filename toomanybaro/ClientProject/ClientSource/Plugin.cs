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


            harmony.Patch(
                original: typeof(PlayerInput).GetMethod("Update"),
                postfix: new HarmonyMethod(typeof(tooManyBaro.ClientSource.KeyBoardInput).GetMethod("onUpdateKeys"))
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
                    tooManyBaro.ClientSource.InventoryPatch.allRecipes.Add(recipe);
                
            }
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
    

    
    
    




}
