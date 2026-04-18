using AcClient;
using Decal.Adapter;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using UtilityBelt.Scripting.Interop;
using UtilityBelt.Service.Lib.Settings;

namespace Aunberean
{
    [FriendlyName("Aunberean")]
    public class PluginCore : PluginBase
    {
        private static string _assemblyDirectory = null;
        public readonly static Game Game = new();
        public VitalUI vitalUI;
        private OptionsUI optionsUI;
        //public WindowUI windowUI;
        public KillTaskUI ktui;
        //public TCblocker tcblocker;
        public WhiteCursor whiteCursor;
        public Settings Settings;

        [Summary("Vital position X")]
        public Setting<int> vitalPosX = new(100);

        [Summary("Vital position Y")]
        public Setting<int> vitalPosY = new(100);

        [Summary("Vital size X")]
        public Setting<int> vitalSizeX = new(350);

        [Summary("Vital size Y")]
        public Setting<int> vitalSizeY = new(180);

        [Summary("Vital Bar")]
        public Setting<bool> vitalBar = new(true);

        [Summary("Simple Vital Bar")]
        public Setting<bool> simpleVitalBar = new(true);

        [Summary("Side by side stamina mana")]
        public Setting<bool> sideBySideStaminaMana = new(true);

        [Summary("Hp Bar Color")]
        public Setting<uint> hpBarColor = new((uint)0xFF0000CC);

        [Summary("Stamina Bar Color")]
        public Setting<uint> staminaBarColor = new(0xFF2fb0F0);

        [Summary("Mana Bar Color")]
        public Setting<uint> manaBarColor = new(0xFFd59300);

        [Summary("Shared cooldown icons")]
        public Setting<bool> vitalBarCooldowns = new(true);

        [Summary("Window Position 1")]
        public Setting<(int x,int y)> windowPosition1 = new((0,0));

        [Summary("Window Position 2")]
        public Setting<(int x, int y)> windowPosition2 = new((0, 0));

        [Summary("Window Position 3")]
        public Setting<(int x, int y)> windowPosition3 = new((0, 0));

        [Summary("Window Position 4")]
        public Setting<(int x, int y)> windowPosition4 = new((0, 0));

        [Summary("Kill Task position X")]
        public Setting<int> ktPosX = new(100);

        [Summary("Kill Task position Y")]
        public Setting<int> ktPosY = new(100);

        [Summary("Kill Task size X")]
        public Setting<int> ktSizeX = new(200);

        [Summary("Kill Task size Y")]
        public Setting<int> ktSizeY = new(100);

        [Summary("Kill Task Tracker")]
        public Setting<bool> ktEnable = new(true);

        [Summary("Kill Task Mark")]
        public Setting<bool> ktMark = new(true);

        [Summary("Kill Task Point")]
        public Setting<bool> ktPoint = new(true);

        [Summary("Hide Kill Task Messages")]
        public Setting<bool> ktHideMessages = new(false);

        [Summary("Filter cloak messages others")]
        public Setting<bool> filterCloakOther = new(true);

        [Summary("Filter cloak messages self")]
        public Setting<bool> filterCloakSelf = new(false);

        [Summary("Filter aetheria messages others")]
        public Setting<bool> filterAetheriaOther = new(true);

        [Summary("Filter aetheria messages self")]
        public Setting<bool> filterAetheriaSelf = new(false);

        [Summary("Corpse Transparency")]
        public Setting<bool> corpseTransparency = new(true);

        [Summary("Corpse Transparency Amount")]
        public Setting<float> corpseTransparencyAmount = new(.7f);

        [Summary("Blocker")]
        public Setting<bool> blockerEnabled = new(true);

        [Summary("Block giving inscibed items to the Town Crier")]
        public Setting<bool> tcBlock = new(true);

        [Summary("Block dropping items from inventory background")]
        public Setting<bool> invBackgroundBlock = new(true);

        [Summary("Block using items on weilded items")]
        public Setting<bool> weildUseBlock = new(true);

        [Summary("Block dropping weilded items")]
        public Setting<bool> weildDropBlock = new(true);

        //[Summary("Block dropping inscribed items")]
        //public Setting<bool> inscibedDropBlock = new(false);

        [Summary("Block using mana stones and tailor kits on inscribed items")]
        public Setting<bool> inscibedUseBlock = new(true);

        [Summary("Block salvaging inscribed items")]
        public Setting<bool> inscibedSalvageBlock = new(true);

        [Summary("Windows Cursors")]
        public Setting<bool> whiteCursors = new(true);
        
        public static string AssemblyDirectory
        {
            get
            {
                if (_assemblyDirectory == null)
                {
                    try
                    {
                        _assemblyDirectory = System.IO.Path.GetDirectoryName(typeof(PluginCore).Assembly.Location);
                    }
                    catch
                    {
                        _assemblyDirectory = Environment.CurrentDirectory;
                    }
                }
                return _assemblyDirectory;
            }
            set
            {
                _assemblyDirectory = value;
            }
        }

        /// <summary>
        /// Called when your plugin is first loaded.
        /// </summary>
        protected override void Startup()
        {
            try
            {
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var settingsPath = System.IO.Path.Combine(documentsPath, "Decal Plugins" , "Aunberean", "settings.json");

                Settings = new Settings(this, settingsPath);
                Settings.Load();
                //ui = new HpBarUI(this);
                //if (vitalBar.Value)
                //{
                //    disableVitalBar();
                //}

                whiteCursor = new WhiteCursor(this);
                if (CoreManager.Current.CharacterFilter.LoginStatus == 3)
                {
                    Init();
                }
                else
                {
                    CoreManager.Current.CharacterFilter.LoginComplete += CharacterFilter_LoginComplete;
                }
                
            }
            catch (Exception ex)
            {
                Log(ex);
            }
        }

        protected void FilterSetup(string assemblyDirectory)
        {
            AssemblyDirectory = assemblyDirectory;
        }

        /// <summary>
        /// CharacterFilter_LoginComplete event handler.
        /// </summary>
        private void CharacterFilter_LoginComplete(object sender, EventArgs e)
        {
            // it's generally a good idea to use try/catch blocks inside of decal event handlers.
            //  throwing an uncaught exception inside one will generally hard crash the client.
            try
            {
                //CoreManager.Current.Actions.AddChatText($"This is my new decal plugin. CharacterFilter_LoginComplete", 1);
                Init();
                
            }
            catch (Exception ex)
            {
                Log(ex);
            }
        }

        private void Init()
        {
            CoreManager.Current.ChatBoxMessage += Current_ChatBoxMessage;
            CoreManager.Current.ContainerOpened += Current_ContainerOpened;
            optionsUI = new OptionsUI(this);
            vitalUI = new VitalUI(this, Game);
            //windowUI = new WindowUI(this);
            ktui = new KillTaskUI(this);
            //tcblocker = new(this, Game);
        }

        public unsafe void disableVitalBar()
        {

            //VITS = 0x100005FA ,
            //SVIT = 0x100006D5,
            var playerSystem = CPlayerSystem.GetPlayerSystem();
            if (playerSystem == null) return;
            
            if (playerSystem->playerModule.PlayerModule.GetOption(PlayerOption.SideBySideVitals_PlayerOption) == 1)
            {
                var vit = CoreManager.Current.Actions.UIElementLookup((Decal.Adapter.Wrappers.UIElementType)0x100006D5);
                if (vit != null)
                {
                    UIElement* ptr = (UIElement*)vit;
                    ptr->SetVisible((byte)(0));
                }
            }
            else
            {
                var vit = CoreManager.Current.Actions.UIElementLookup(Decal.Adapter.Wrappers.UIElementType.Vitals);
                if (vit != null)
                {
                    UIElement* ptr = (UIElement*)vit;
                    ptr->SetVisible((byte)(0));
                }
            }
            
        }

        unsafe private void Current_ContainerOpened(object sender, ContainerOpenedEventArgs e)
        {
            if (!corpseTransparency.Value) return;
            int objectId = e.ItemGuid;
            if (CoreManager.Current.WorldFilter[objectId].ObjectClass != Decal.Adapter.Wrappers.ObjectClass.Corpse) return;

            CPhysicsObj* ptr = (CPhysicsObj*)CoreManager.Current.Actions.Underlying.GetPhysicsObjectPtr((int)objectId);
            if (ptr != null)
            {
                var t = ptr->translucency;
                if (t == 0f)
                {
                    ptr->SetTranslucency(corpseTransparencyAmount.Value, 1.0);
                }
            }

        }
        private void Current_ChatBoxMessage(object sender, ChatTextInterceptEventArgs e)
        {
            try
            {
                if (e.Eat || string.IsNullOrEmpty(e.Text))
                    return;

                //Aetheria surges on Lohan with the power of Surge of Protection!
                //Aetheria surges on Aun with the power of Surge of Destruction!
                //Aetheria surges on Aun with the power of Surge of Protection!
                //Aetheria surges on Copper Target Drudge with the power of Surge of Affliction!
                //Aetheria surges on Aun with the power of Surge of Regeneration!
                //Aetheria surges on Copper Target Drudge with the power of Surge of Affliction!

                if (e.Eat == false && filterCloakOther.Value)
                {
                    if (e.Text.StartsWith("The cloak of ") && e.Text.Contains(" weaves the magic of ") && !e.Text.Contains(" " + CoreManager.Current.CharacterFilter.Name + " "))
                       { 
                        e.Eat = true;
                        return;
                    }

                }

                if (e.Eat == false && filterCloakSelf.Value)
                {
                    if (e.Text.StartsWith("The cloak of ") && e.Text.Contains(" weaves the magic of ") && e.Text.Contains(" " + CoreManager.Current.CharacterFilter.Name + " "))
                        { 
                        e.Eat = true;
                        return;
                    }
                }

                if (e.Eat == false && filterAetheriaOther.Value)
                {
                    if (e.Text.StartsWith("Aetheria surges on ") && e.Text.Contains(" with the power of ") && !e.Text.Contains(" " + CoreManager.Current.CharacterFilter.Name + " "))
                        { 
                        e.Eat = true;
                        return;
                    }
                }

                if (e.Eat == false && filterAetheriaSelf.Value)
                {
                    if (e.Text.StartsWith("Aetheria surges on ") && e.Text.Contains(" with the power of ") && e.Text.Contains(" " + CoreManager.Current.CharacterFilter.Name + " "))
                        { 
                        e.Eat = true;
                        return;
                    }
                    if (e.Text.StartsWith("Surge of ") && e.Text.EndsWith(" has expired."))
                    {
                        e.Eat = true;
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Log(ex);
            }
        }

        /// <summary>
        /// Called when your plugin is unloaded. Either when logging out, closing the client, or hot reloading.
        /// </summary>
        protected override void Shutdown()
        {
            try
            {
                // make sure to unsubscribe from any events we were subscribed to. Not doing so
                // can cause the old plugin to stay loaded between hot reloads.
                CoreManager.Current.CharacterFilter.LoginComplete -= CharacterFilter_LoginComplete;
                CoreManager.Current.ChatBoxMessage -= Current_ChatBoxMessage;
                CoreManager.Current.ContainerOpened -= Current_ContainerOpened;
                // clean up our ui view
                //tcblocker.Dispose();
                whiteCursor.Dispose();
                vitalUI.Dispose();
                optionsUI.Dispose();
                //windowUI.Dispose();
                ktui.Dispose();

                if (Settings != null)
                {
                    if (Settings.NeedsSave)
                    {
                        Settings.Save();
                    }
                }
            }
            catch (Exception ex)
            {
                Log(ex);
            }
        }

        #region logging
        /// <summary>
        /// Log an exception to log.txt in the same directory as the plugin.
        /// </summary>
        /// <param name="ex"></param>
        internal static void Log(Exception ex)
        {
            Log(ex.ToString());
        }

        /// <summary>
        /// Log a string to log.txt in the same directory as the plugin.
        /// </summary>
        /// <param name="message"></param>
        internal static void Log(string message)
        {
            try
            {
                File.AppendAllText(System.IO.Path.Combine(AssemblyDirectory, "log.txt"), $"{message}\n");

                CoreManager.Current.Actions.AddChatText(message, 1);
            }
            catch { }
        }
        #endregion // logging
    }
}
