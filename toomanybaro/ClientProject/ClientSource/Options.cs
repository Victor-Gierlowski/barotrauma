using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Xml;
using System.Xml.Linq;
using Barotrauma;
using Microsoft.Xna.Framework;

namespace tooManyBaro.ClientSource
{

    class Options
    {

        /// <summary>
        /// Subclass use to store and use values of options.
        /// </summary>
        public class option
        {

            private int _refresh_time;
            public int refresh_time
            {
                get => _refresh_time;
                set
                {
                    //DebugConsole.NewMessage($"Updating with {value}");
                    if (value >= 100 && value < 1e7)
                    {
                        _refresh_time = value;
                    }
                    //DebugConsole.NewMessage($"{_refresh_time}");
                }
            }

            public bool reopen_recipes_after_close;
        }


        /// <summary>
        /// Option filename. NOT LOCATION => is determine within the execution base on the folder of either local or workshop.
        /// </summary>
        public static string USER_OPTION_FILE = "TooManyBaro_Options.xml";
        public static option? defaultOptions;
        public static option? userOptions;
        private static DateTime timeCallSave;
        public static System.Timers.Timer? __reminder_to_save_options;

        private const int DEFAULT_refresh_time = 2000;
        public static int refresh_time
        {
            get
            {
                if (userOptions == null)
                {
                    if (defaultOptions == null) return DEFAULT_refresh_time;
                    else return defaultOptions.refresh_time;
                }
                return userOptions.refresh_time;
            }
            set
            {
                if (userOptions != null)
                {
                    userOptions.refresh_time = value;
                    saveOptions();
                }
                else
                {
                    userOptions = new option();
                    if (defaultOptions != null) defaultOptions.CopyValuesTo(userOptions);
                }
            }
        }

        private const bool DEFAULT_reopen_recipes_after_close = false;
        public static bool reopen_recipes_after_close
        {
            get
            {
                if (userOptions == null)
                {
                    if (defaultOptions == null) return DEFAULT_reopen_recipes_after_close;
                    else return defaultOptions.reopen_recipes_after_close;
                }
                return userOptions.reopen_recipes_after_close;
            }
            set
            {
                if (userOptions != null)
                {
                    userOptions.reopen_recipes_after_close = value;
                    saveOptions();
                }
                else
                {
                    userOptions = new option();
                    if (defaultOptions != null) defaultOptions.CopyValuesTo(userOptions);
                }
            }
        }

        /// <summary>
        /// Main function to call and load the options. Will generate the 2 objects, user and default option.
        /// </summary>
        public static void loadOptions()
        {

            //userOptions = new option();
            //userOptions.refresh_time = 2000;
            //defaultOptions = new option();
            //userOptions.CopyValuesTo(defaultOptions);

            if (__reminder_to_save_options == null) __reminder_to_save_options = new System.Timers.Timer(1000);
            __reminder_to_save_options.AutoReset = false;
            __reminder_to_save_options.Elapsed += save_reminder;
            defaultOptions = loadDefault();
            userOptions = loadUserOptions();
            if (userOptions == null)
            {
                userOptions = new option();
                defaultOptions.CopyValuesTo(userOptions);
                saveOptions();
            }
        }

        /// <summary>
        /// the function periodically called by the timer if a save need to happen.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void save_reminder(object? sender, ElapsedEventArgs e)
        {
            saveOptions();
        }


        /// <summary>
        /// try to load the option file containing the user defined options.
        /// </summary>
        /// <returns>object defined either with file value or default one.</returns>
        public static option loadUserOptions()
        {
            option uOptions= new option();
                if (!File.Exists(USER_OPTION_FILE)) return null;
            XDocument xmlDefaultOptions = XDocument.Load(USER_OPTION_FILE);
            loadGUIOptions(xmlDefaultOptions.Descendants("GUI").FirstOrDefault(), uOptions);
            return uOptions;
        }

        /// <summary>
        /// Save the options of the user to the file. If function  has been called too recently it will be cancel and instead ask for a reminder.
        /// </summary>
        /// <returns></returns>
        public static bool saveOptions()
        {
            TimeSpan elapsed = DateTime.Now - timeCallSave;
            if (!(elapsed.TotalMilliseconds >= 2000))
            {
                if (__reminder_to_save_options == null || __reminder_to_save_options.Enabled == true) return false;
                __reminder_to_save_options.Enabled = true;
            }
            timeCallSave = DateTime.Now;
            if (userOptions == null)
            {
                CRY("Saving but there is not option object");
                return false;
            }
            XmlDocument xmlDoc = new();
            XmlDeclaration xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
            XmlElement root = xmlDoc.DocumentElement;
            xmlDoc.InsertBefore(xmlDeclaration, root);
            XmlElement configElement = xmlDoc.CreateElement(string.Empty, "config", string.Empty);
            xmlDoc.AppendChild(configElement);
            XmlElement guiElement = xmlDoc.CreateElement(string.Empty, "GUI", string.Empty);
            XmlAttribute refreshTimeAttr = xmlDoc.CreateAttribute(string.Empty, "refresh_time", string.Empty);
            XmlAttribute reopenAttr = xmlDoc.CreateAttribute(string.Empty, "reopen_recipes_after_close", string.Empty);
            reopenAttr.Value = $"{userOptions.reopen_recipes_after_close}";
            refreshTimeAttr.Value = $"{userOptions.refresh_time}";
            guiElement.Attributes.Append(refreshTimeAttr);
            guiElement.Attributes.Append(reopenAttr);
            configElement.AppendChild(guiElement);
            try
            {
                xmlDoc.Save(USER_OPTION_FILE);
            }catch(XmlException err)
            {
                CRY($"{err.Message} | couldn't save the options.");
                return false;
            }
            return true;
        }

        public static option loadDefault()
        {
            option defoptions= new option();
            var mod = ContentPackageManager.LocalPackages.First(mod => mod.Name == "toomanybaro");
            if (mod == null)
                mod = ContentPackageManager.WorkshopPackages.First(mod => mod.Name == "toomanybaro");
            if (mod == null)
            {
                CRY("No mods package. ? ");
                return null;
            }
            USER_OPTION_FILE = $"{mod.Dir}/{USER_OPTION_FILE}";
            string file = mod.Dir + "/Content/Options/default_options.xml";

            foreach (var i in ContentPackageManager.WorkshopPackages)
            {
                DebugConsole.NewMessage($"{i.Name}");
            }
            ;
            XDocument xmlDefaultOptions = XDocument.Load(file);
            if (xmlDefaultOptions != null)
            {
                //CRY("loading option file");
                loadGUIOptions(xmlDefaultOptions.Descendants("GUI").FirstOrDefault(), defoptions);
            }
            else CRY("option file MISSING!:!");

            return defoptions;
        }

        private static void CRY(String text)
        {
            DebugConsole.NewMessage($"tooManyBaro[LoadingOptions]:{text}",color:Color.Red);
        }


        public static void loadGUIOptions(XElement? goption, option optionTarget)
        {
            int refresh_time;
            if (goption == null)
            {
                optionTarget.refresh_time = DEFAULT_refresh_time;
                optionTarget.reopen_recipes_after_close = DEFAULT_reopen_recipes_after_close;
                return;
            }
            var rfile = goption.Attribute("refresh_time")?.Value;
            if (rfile != null) {
                refresh_time = int.Parse(rfile);
                if (refresh_time < 100 || refresh_time > 1e7)
                    refresh_time = DEFAULT_refresh_time;
            } else refresh_time = DEFAULT_refresh_time;
            optionTarget.refresh_time = refresh_time;

            var reopenrecipes = goption.Attribute("reopen_recipes_after_close")?.Value;
            bool reopen_recipes_after_close = false;
            if (reopenrecipes != null)
            {
                reopen_recipes_after_close = bool.Parse(reopenrecipes);
            }
            optionTarget.reopen_recipes_after_close = reopen_recipes_after_close;
        }

    }
}
