using Barotrauma;
using Barotrauma.Extensions;
using Microsoft.Xna.Framework;
using tooManyBaro.ClientSource;
using GUI = tooManyBaro.ClientSource.GUI;

namespace toomanybaro.ClientSource
{
    class GUIrecipes
    {


        public static GUILayoutGroup? itemListFrame;
        public static GUILayoutGroup? paddedItemFrame;
        public static GUILayoutGroup? usageFrame;
        public static GUILayoutGroup? paddedUsageFrame;

        /// <summary>
        /// Main function to draw the recipes tab. Will call sub function like <see cref="AddRecipesToListFilter(List{FabricationRecipe}, RectTransform)"/> to separate recipes that need special craft.
        /// </summary>
        /// <param name="wholeframe"></param>
        /// <param name="topFrame"></param>
        public static void drawRecipes(GUIFrame wholeframe, GUIFrame topFrame)
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
            GUI.allGUIComponents.Add(wholeframe);
            GUI.allGUIComponents.Add(deconsframe);
            GUI.allGUIComponents.Add(deconsList);
            AddRecipesToListFilter(InventoryPatch.Producers, itemList.Content.rectTransform);
            AddRecipesToListFilter(InventoryPatch.Usages, itemListUsage.Content.rectTransform);
            GUI.AddDeconstructToList(InventoryPatch.DeconstructItems, deconsListSub.rectTransform);
            GUI.allGUIComponents.Add(itemListFrame);
            GUI.allGUIComponents.Add(paddedItemFrame);
            GUI.allGUIComponents.Add(usageFrame);
        }


        /// <summary>
        /// Draw all the recipes given inside the specified target.
        /// </summary>
        /// <param name="l"></param>
        /// <param name="targetRect"></param>
        /// <param name="producewhenDecons"></param>
        public static void AddRecipesToListFilter(List<FabricationRecipe> l, RectTransform targetRect)
        {
            var lrecipeNeeded = (List<FabricationRecipe>)l.Where(static r => { return r.RequiresRecipe == true; }).ToList();
            var paddednrnf = new GUIListBox(new RectTransform(new Vector2(1f, 0.7f), targetRect), style: "ItemUI");
            var noRecipeNeededFrame = new GUIListBox(new RectTransform(new Vector2(0.9f, 0.9f), paddednrnf.Content.rectTransform, anchor: Anchor.Center));
            List<FabricationRecipe>myL = l.Where(static r => { return r.RequiresRecipe == false; }).ToList();
            AddRecipesToList(myL, noRecipeNeededFrame.Content.RectTransform);
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
                        String conditionTooltip = GUI.getConditionTooltip(ip);
                        bool needCondition = conditionTooltip.Length > 0;

                        RichString tooltip = conditionTooltip + curr_item_draw.GetTooltip(Character.controlled);
                        var img = new GUIImage(new RectTransform(new Vector2(1f, 1.0f), itemDeconsIconBox.rectTransform), itemIcon, scaleToFit: true)
                        {
                            Color = curr_item_draw.SpriteColor,
                            toolTip = RichString.Rich(tooltip),
                        };
                        if (needToBeSwap)
                            GUI.item_swap_over_time.Add((img, ip));
                        // WRITE CONDITION OVER ITEM
                        if (needCondition)
                        {
                            var cmin = GUI.getItemConditionColor(ip.MinCondition);
                            var cmax = GUI.getItemConditionColor(ip.MaxCondition);
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
                        GUI.imageToItem.Add((img, curr_item_draw));
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
                    GUI.imageToItem.Add((img, recipe.TargetItem));
                    new GUITextBlock(new RectTransform(new Vector2(0.0f, 1f), rrecipe.Content.rectTransform), $"x{recipe.Amount}", textAlignment: Alignment.BottomRight);
                }
                GUI.allGUIComponents.Add(recipeLine);
                GUI.allGUIComponents.Add(lrecipe);
                GUI.allGUIComponents.Add(rrecipe);
            }
        }
    }
}
