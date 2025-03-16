using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Barotrauma;
using HarmonyLib;
using Microsoft.Xna.Framework;



namespace tooManyBaro
{
    class toomanybaro : IAssemblyPlugin
    {
        public Harmony harmony;
        public void Initialize()
        {
            harmony = new Harmony("com.barotrauma.toomanybaro");
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

        }
        public void OnLoadCompleted() { }
        public void PreInitPatching() { }
        public void Dispose()
        {
            harmony.UnpatchAll();
            harmony = null;
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
            if (mouseOn)
            {
                DebugConsole.NewMessage($"Mouse on slot: {mouseOn} i:{slotIndex} sub:{isSubSlot}", Color.Gold);
                DebugConsole.NewMessage($"Item name: {item?.Name}",Color.Blue);
            }
        }
    }



}
