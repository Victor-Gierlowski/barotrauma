using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Barotrauma;
using Microsoft.Xna.Framework.Input;

namespace tooManyBaro.ClientSource
{

    class KeyBoardInput
    {
        /// <summary>
        /// Because i couldn't find any simpler way to listen for keyboard input.
        /// After the core game update, I check too for my inputs.
        /// </summary>
        /// <param name="deltaTime"></param>
        public static void onUpdateKeys(double deltaTime)
        {
            Keys[] kPressed = PlayerInput.keyboardState.GetPressedKeys();
            foreach (Keys key in kPressed)
            {
                if (key == Keys.O)
                    tooManyBaro.ClientSource.InventoryPatch.checkInput();
            }
            var lftclick = PlayerInput.PrimaryMouseButtonClicked();
            var rghclick = PlayerInput.SecondaryMouseButtonClicked();
            if (lftclick || rghclick)
                tooManyBaro.ClientSource.GUI.mouseClicked(lftclick);
        }
    }

}
