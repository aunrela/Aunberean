using AcClient;
using ACE.Entity.Models;
using CommandLine;
using Decal.Adapter;
using Decal.Adapter.Wrappers;
using ImGuiNET;
using Microsoft.DirectX.PrivateImplementationDetails;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UtilityBelt.Common.Enums;
using UtilityBelt.Scripting.Enums;
using UtilityBelt.Scripting.Interop;
using UtilityBelt.Service.Lib.Settings;

using ObjectClass = UtilityBelt.Scripting.Enums.ObjectClass;

namespace Aunberean
{
    public class TCblocker : IDisposable
    {
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate int Del_UIElement_CatchDroppedItem(IntPtr This, IntPtr info);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate void HandleTargetedUseLeftClick(IntPtr _this, IntPtr item);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate void HandleTargetedUseLeftClickSmartBox(IntPtr _this, uint itemm, uint item2);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate bool IsItemSuitable(IntPtr _this, IntPtr item);

        internal static Hook2 hookCatchDroppedItem = new Hook2((int)Entrypoint.UIElement__CatchDroppedItem, (int)Call.UIElement__CatchDroppedItem_0x0047283C);
        internal static Hook2 hookTargetedUse = new Hook2((int)Entrypoint.UIElement_ItemList__HandleTargetedUseLeftClick, (int)Call.UIElement_ItemList__HandleTargetedUseLeftClick_0x004E5A37);
        internal static Hook2 hookTargetedUseSmartBox = new Hook2((int)Entrypoint.UIElement_SmartBoxWrapper__HandleTargetedUseLeftClick, (int)Call.UIElement_SmartBoxWrapper__HandleTargetedUseLeftClick_0x004E6982);
        internal static Hook2 hookIsItemSuitable = new Hook2((int)Entrypoint.gmSalvageUI__IsItemSuitable, (int)Call.gmSalvageUI__IsItemSuitable_0x004CBEEF);
        internal static Hook2 hookIsItemSuitable2 = new Hook2((int)Entrypoint.gmSalvageUI__IsItemSuitable, (int)Call.gmSalvageUI__IsItemSuitable_0x004CC315);

        private readonly PluginCore _plugin;
        private readonly Game _game;

        bool tcTempBlock = false;

        public TCblocker(PluginCore plugin, Game game)
        {
            _plugin = plugin;
            _game = game;

            hookCatchDroppedItem.Setup(new Del_UIElement_CatchDroppedItem(UIElement_CatchDroppedItem_Impl), typeof(Del_UIElement_CatchDroppedItem));
            hookTargetedUse.Setup(new HandleTargetedUseLeftClick(myHandleTargetedUseLeftClick), typeof(HandleTargetedUseLeftClick));
            hookTargetedUseSmartBox.Setup(new HandleTargetedUseLeftClickSmartBox(myHandleTargetedUseLeftClickSmartBox), typeof(HandleTargetedUseLeftClickSmartBox));

            //hookIsItemSuitable.Setup(new IsItemSuitable(myIsItemSuitable), typeof(IsItemSuitable));
            //hookIsItemSuitable2.Setup(new IsItemSuitable(myIsItemSuitable2), typeof(IsItemSuitable));
        }

        public unsafe bool myIsItemSuitable(IntPtr _this, IntPtr item)
        {
            try
            {
                var orig = (IsItemSuitable)hookIsItemSuitable.Original;

                if (!_plugin.blockerEnabled.Value || !_plugin.inscibedSalvageBlock.Value)
                    return orig(_this, item);

                var weenie = (ACCWeenieObject*)item;
                var itemWo = _game.World[weenie->a0.a0.a0.id];
                if (itemWo == null) return false;
                if (!itemWo.HasAppraisalData)
                {
                    CoreManager.Current?.Actions?.AddChatText($"[Aunberean] Blocked salvage of unappraised item.", 5);
                    var re = _game.Actions.ObjectAppraise(itemWo.Id);
                    re.OnFinished += Re_OnFinished;
                    return false;
                }
                if (itemWo.HasValue(StringId.Inscription) && itemWo.Value(StringId.Inscription) != "")
                {
                    CoreManager.Current?.Actions?.AddChatText($"[Aunberean] Blocked salvage of inscribed item.", 5);
                    itemWo.StringValues.Remove(StringId.Inscription);
                    tcTempBlock = true;
                    var re = _game.Actions.ObjectAppraise(itemWo.Id);
                    re.OnFinished += Re_OnFinished;
                    return false;
                }

                //CoreManager.Current?.Actions?.AddChatText($"[Aunberean] saw " + itemWo.Name, 5);


                return orig(_this, item);
            }
            catch (Exception ex)
            {
                Log(ex);
                return false;
            }
        }

        public unsafe bool myIsItemSuitable2(IntPtr _this, IntPtr item)
        {
            try
            {
                var orig = (IsItemSuitable)hookIsItemSuitable2.Original;

                if (!_plugin.blockerEnabled.Value || !_plugin.inscibedSalvageBlock.Value)
                    return orig(_this, item);

                var weenie = (ACCWeenieObject*)item;
                //var id = weenie->a0
                //CoreManager.Current?.Actions?.AddChatText($"[Aunberean] saw " + weenie->a0.a0.a0.id.ToString(), 5);

                var itemWo = _game.World[weenie->a0.a0.a0.id];
                if (itemWo == null) return false;
                if (!itemWo.HasAppraisalData)
                {
                    CoreManager.Current?.Actions?.AddChatText($"[Aunberean] Blocked salvage of unappraised item.", 5);
                    var re = _game.Actions.ObjectAppraise(itemWo.Id);
                    re.OnFinished += Re_OnFinished;
                    return false;
                }
                if (itemWo.HasValue(StringId.Inscription) && itemWo.Value(StringId.Inscription) != "")
                {
                    CoreManager.Current?.Actions?.AddChatText($"[Aunberean] Blocked salvage of inscribed item.", 5);
                    itemWo.StringValues.Remove(StringId.Inscription);
                    tcTempBlock = true;
                    var re = _game.Actions.ObjectAppraise(itemWo.Id);
                    re.OnFinished += Re_OnFinished;
                    return false;
                }
                //CoreManager.Current?.Actions?.AddChatText($"[Aunberean] saw 2 " + itemWo.Name, 5);

                return orig(_this, item);
            }
            catch (Exception ex)
            {
                Log(ex);
                return false;
            }
        }
        List<UtilityBelt.Scripting.Enums.ObjectClass> tcBlockClasses = new List<UtilityBelt.Scripting.Enums.ObjectClass>
        {
            ObjectClass.MeleeWeapon,
            ObjectClass.Armor,
            ObjectClass.Clothing,
            ObjectClass.Jewelry,
            ObjectClass.Misc, // pets?
            ObjectClass.MissileWeapon,
            //ObjectClass.Gem, // Aetheria
            ObjectClass.WandStaffOrb,
            ObjectClass.TradeNote,
            //ObjectClass.Key,
            //ObjectClass.
        };
        List<string> tcBlockNames = new List<string>{
            "Atheria",
        };

        List<string> tcAlwaysBlockNames = new List<string>{
            "Low-Stakes Gambling Token",
            "Mid-Stakes Gambling Token",
            "High-Stakes Gambling Token",
            "Deed",
            "Colosseum Coin",
            "Ornate Gear Marker",
            "Small Olthoi Venom Sac",
            "Ancient Mhoire Coin",
            "Black Opal Foolproof",
            "Sunstone Foolproof",
        };
        public unsafe void myHandleTargetedUseLeftClick(IntPtr This, IntPtr item)
        {
            try
            {
                var orig = (HandleTargetedUseLeftClick)hookTargetedUse.Original;

                if (!_plugin.blockerEnabled.Value)
                {
                    orig(This, item);
                    return;
                }

                var UIElement_UIItem = (UIElement_UIItem*)item;
                var itemWo = _game.World[UIElement_UIItem->itemID];

                if (itemWo.HasValue(IntId.CurrentWieldedLocation))
                {
                    var weildedlocs = itemWo.Value(IntId.CurrentWieldedLocation);
                    if (weildedlocs != 0 && _plugin.weildUseBlock.Value)
                    {
                        CoreManager.Current?.Actions?.AddChatText($"[Aunberean] Blocked use on wielded item.", 5);
                        return;
                    }

                }

                if (tcTempBlock) return;

                uint* targetingObject = (uint*)0x00871AE4;
                uint targetID = *targetingObject;
                var usedItemWo = _game.World[targetID];
                if (usedItemWo != null && _plugin.inscibedUseBlock.Value)
                {
                    if (usedItemWo.ObjectType == ObjectType.ManaStone
                        || usedItemWo.Name == "Weapon Tailoring Kit"
                        || usedItemWo.Name == "Armor Tailoring Kit")
                    {
                        //CoreManager.Current?.Actions?.AddChatText($"used " + usedItemWo.Name, 1);

                        if (tcBlockClasses.Contains(itemWo.ObjectClass) || tcBlockNames.Contains(itemWo.Name))
                        {
                            if (!itemWo.HasAppraisalData)
                            {
                                CoreManager.Current?.Actions?.AddChatText($"[Aunberean] Blocked use, item not appraised.", 5);
                                var re = _game.Actions.ObjectAppraise(itemWo.Id);
                                re.OnFinished += Re_OnFinished;
                                return;
                            }
                            if (itemWo.HasValue(StringId.Inscription) && itemWo.Value(StringId.Inscription) != "")
                            {
                                CoreManager.Current?.Actions?.AddChatText($"[Aunberean] Blocked use, item has insciption.", 5);
                                itemWo.StringValues.Remove(StringId.Inscription);
                                tcTempBlock = true;
                                var re = _game.Actions.ObjectAppraise(itemWo.Id);
                                re.OnFinished += Re_OnFinished;
                                return;
                            }
                        }
                    }
                }

                orig(This, item);
                return;
            }
            catch (Exception ex)
            {
                Log(ex);
                return;
            }
        }

        public unsafe void myHandleTargetedUseLeftClickSmartBox(IntPtr This, uint itemID, uint mode)
        {
            try
            {
                var orig = (HandleTargetedUseLeftClickSmartBox)hookTargetedUseSmartBox.Original;

                if (!_plugin.blockerEnabled.Value || !_plugin.weildUseBlock.Value)
                {
                    orig(This, itemID, mode);
                    return;
                }

                var itemWo = _game.World[itemID];
                if (itemWo != null)
                {
                    //CoreManager.Current?.Actions?.AddChatText($"used on " + itemWo.Name, 1);
                    if (itemWo.HasValue(IntId.CurrentWieldedLocation))
                    {
                        var weildedlocs = itemWo.Value(IntId.CurrentWieldedLocation);
                        if (weildedlocs != 0)
                        {
                            CoreManager.Current?.Actions?.AddChatText($"[Aunberean] Blocked use on wielded item.", 5);
                            return;
                        }
                    }
                }

                orig(This, itemID, mode);
                return;
            }
            catch (Exception ex)
            {
                Log(ex);
                return;
            }

        }
        private unsafe int UIElement_CatchDroppedItem_Impl(IntPtr This, IntPtr info)
        {
            try
            {
                var orig = (Del_UIElement_CatchDroppedItem)hookCatchDroppedItem.Original;

                if (!_plugin.blockerEnabled.Value)
                    return orig(This, info);

                uint itemId = 0;
                uint spellId = 0;
                DropItemFlags flags;

                var UIElement = (UIElement*)This;
                var DragDropInfo = (DragDropInfo*)info;

                UIElement_ItemList.InqDropIconInfo(DragDropInfo->element, &itemId, &spellId, &flags);

                if (itemId == 0)
                    return orig(This, info);

                //var UstPtr = CoreManager.Current.Actions.UIElementLookup((UIElementType)0x10000011);

                //if (UstPtr != null || This == UstPtr)
                //    CoreManager.Current?.Actions?.AddChatText("[Aunberean] ust?", 5);

                var smartBoxPtr = CoreManager.Current.Actions.UIElementLookup(UIElementType.Smartbox);

                if (smartBoxPtr == null || This != smartBoxPtr)
                    return orig(This, info);

                //UIElement* smartBox = (UIElement*)smartBoxPtr;

                var invptr = CoreManager.Current.Actions.UIElementLookup(UIElementType.Panels);

                if (invptr != null && _plugin.invBackgroundBlock.Value)
                {

                    UIElement* inventoryUI = (UIElement*)invptr;
                    if (inventoryUI->IsVisible() == 1)
                    {
                        Box2D box = new();
                        int zlevel = 0;

                        inventoryUI->GetCurrentPosition(&box, &zlevel);

                        var cursorpos = GetCursorPosition();
                        if (box.m_x0 <= cursorpos.X && cursorpos.X <= box.m_x1
                            && box.m_y0 <= cursorpos.Y && cursorpos.Y <= box.m_y1)
                        {
                            CoreManager.Current?.Actions?.AddChatText("[Aunberean] Blocked drop in inventory background", 5);
                            return 0;
                        }

                    }
                }

                var itemWo = _game.World[itemId];
                if (itemWo.HasValue(IntId.CurrentWieldedLocation) &&
                    itemWo.Value(IntId.CurrentWieldedLocation) != 0 &&
                    _plugin.weildDropBlock.Value)
                {
                    CoreManager.Current?.Actions?.AddChatText($"[Aunberean] Blocked drop of wielded item.", 5);
                    return 0;
                }

                var targetid = SmartBox.get_found_object_id();
                if (targetid == 0) 
                    return orig(This, info);

                var wo = _game.World[targetid];
                if (wo == null) 
                    return orig(This, info);

                if (tcTempBlock) 
                    return 0;

                if (_plugin.tcBlock.Value && 
                    (wo.Name == "Town Crier" || wo.Name == "Garbage Barrel"))
                {
                    if (tcAlwaysBlockNames.Contains(itemWo.Name))
                    {
                        CoreManager.Current?.Actions?.AddChatText($"[Aunberean] Blocked give of item.", 5);
                        return 0;
                    }

                    if (tcBlockClasses.Contains(itemWo.ObjectClass) || tcBlockNames.Contains(itemWo.Name))
                    {
                        if (!itemWo.HasAppraisalData)
                        {
                            CoreManager.Current?.Actions?.AddChatText($"[Aunberean] Blocked give of unappraised item.", 5);
                            var re = _game.Actions.ObjectAppraise(itemWo.Id);
                            re.OnFinished += Re_OnFinished;
                            return 0;
                        }
                        if (itemWo.HasValue(StringId.Inscription) && itemWo.Value(StringId.Inscription) != "")
                        {
                            CoreManager.Current?.Actions?.AddChatText($"[Aunberean] Blocked give of inscribed item.", 5);
                            itemWo.StringValues.Remove(StringId.Inscription);
                            tcTempBlock = true;
                            var re = _game.Actions.ObjectAppraise(itemWo.Id);
                            re.OnFinished += Re_OnFinished;
                            return 0;
                        }
                    }

                }
                return orig(This, info);
            }
            catch (Exception ex)
            {
                Log(ex);
                return 0;
            }
        }

        private void Re_OnFinished(object sender, EventArgs e)
        {

            CoreManager.Current?.Actions?.AddChatText($"[Aunberean] Finished appraisal.", 5);
            tcTempBlock = false;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public static implicit operator Point(POINT point)
            {
                return new Point(point.X, point.Y);
            }
        }

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        public static Point GetCursorPosition()
        {
            POINT lpPoint;
            GetCursorPos(out lpPoint);
            return lpPoint;
        }

        public void Dispose()
        {
            hookCatchDroppedItem.Remove();
            hookTargetedUse.Remove();
            hookTargetedUseSmartBox.Remove();

            hookIsItemSuitable.Remove();
            hookIsItemSuitable2.Remove();
        }

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
                File.AppendAllText(System.IO.Path.Combine(PluginCore.AssemblyDirectory, "log.txt"), $"{message}\n");

                CoreManager.Current.Actions.AddChatText(message, 1);
            }
            catch { }
        }
    }
}
