using AcClient;
using ACE.DatLoader.FileTypes;
using Decal.Adapter;
using Decal.Adapter.Wrappers;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using UtilityBelt.Common.Enums;
using UtilityBelt.Scripting.Enums;
using UtilityBelt.Scripting.Interop;
using UtilityBelt.Service;
using UtilityBelt.Service.Lib.Settings;
using UtilityBelt.Service.Views;
using WattleScript.Interpreter;
using static System.Net.Mime.MediaTypeNames;
using Hud = UtilityBelt.Service.Views.Hud;
using ObjectClass = UtilityBelt.Scripting.Enums.ObjectClass;

namespace Aunberean
{
    public class HealKitUI : IDisposable
    {
        private readonly Hud hud;
        PluginCore _plugin;
        public Settings Settings;
        private OneTouchHeal oneTouchHeal;
        private bool hovered;
        bool help = false;
        public Dictionary<uint, ManagedTexture> icons;

        [Summary("HealKits")]
        public Setting<List<HealKit>> healKits = new(new() {
            new HealKit ("Treated Healing Kit",25,2.0f,9229u,13029u,false,97.0f),
            new HealKit ("Plentiful Healing Kit",100,1.6f,22449u,10504u,false,97.0f),
            new HealKit ("Light Infused Healing Kit",250,2.0f,43479u,13029u,false,94.0f),
            new HealKit ("Renegade Herbal Kit",200,1.0f,27671u,13227u,false,97.0f),
            new HealKit ("Enhanced Health Elixir",200,0.0f,37517u,13016u,true,0.0f),
            new HealKit ("Black Market Health Elixir",300,0.0f,38794u,13016u,true,0.0f),
            new HealKit ("Health Philtre",100,0.0f,27318u,13018u,true,0.0f),
            new HealKit ("Renegade Herbal Kit",200,1.0f,27671u,13227u,false,0.0f),
            new HealKit ("Plentiful Healing Kit",100,1.6f,22449u,10504u,false,0.0f),
            new HealKit ("Treated Healing Kit",25,2.0f,9229u,13029u,false,0.0f)});

        VirindiHotkeySystem.VHotkeyInfo oneTouchHealHotkey = new VirindiHotkeySystem.VHotkeyInfo("Aunberean", true, "Heal Kit Hotkey", "Uses heal kits", 0, false, false, false);

        public HealKitUI(PluginCore plugin)
        {
            try
            {
                _plugin = plugin;
                icons = new Dictionary<uint, ManagedTexture>();

                hud = UBService.Huds.CreateHud("Heal Kit Hotkey", new ManagedTexture(13029u).Bitmap);
                hud.ShowInBar = !_plugin.hideHealKitIcon.Value;
                hud.Visible = false;
                hud.OnRender += Hud_OnRender;

                string settingsPath;
                //if (_plugin.perCharacterHealKit.Value)
                //{
                //    string documentsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
                //    string serverName = PluginCore.game.ServerName;
                //    string accountName = PluginCore.game.AccountName;
                //    string characterName = CoreManager.Current.CharacterFilter.Name;
                //    string settingsDirectory = Path.Combine(
                //            documentsPath,
                //            "Decal Plugins",
                //            "Aunberean",
                //            serverName,
                //            accountName,
                //            characterName
                //        );
                //    settingsPath = Path.Combine(
                //            settingsDirectory,
                //            "HealSettings.json"
                //        );
                //    Directory.CreateDirectory(settingsDirectory);
                //}
                //else
                //{
                    string documentsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
                    settingsPath = System.IO.Path.Combine(documentsPath, "Decal Plugins", "Aunberean", "HealSettings.json");
                //}
                
                Settings = new Settings(this, settingsPath);
                Settings.Load();

                oneTouchHeal = new OneTouchHeal(healKits.Value);

                VirindiHotkeySystem.VHotkeySystem.InstanceReal.AddHotkey(oneTouchHealHotkey);

                oneTouchHealHotkey.Fired2 += OneTouchHealHotkey_Fired2;
                CoreManager.Current.WindowMessage += Current_WindowMessage;
            }
            catch
            (Exception)
            {
               
            }
        }
        DragDropPayload _lastDragDropInfo;
        private unsafe void Current_WindowMessage(object sender, WindowMessageEventArgs e)
        {
            if (e.Msg != 514) return;

            if (!hud.Visible) return;

            if (hovered == false) return;

            var dragEl = UIElementManager.s_pInstance->m_dragElement;

            if (dragEl is not null)
            {
                _lastDragDropInfo = IconHelpers.GetDragDropInfo(dragEl);
                fixed (DragDropPayload* info = &_lastDragDropInfo)
                {
                    if (info->ItemId != 0)
                    {
                        UIElementManager.s_pInstance->ClearDragandDrop();
                        UIElementManager.s_pInstance->StopDragandDrop();

                        var ptrr = IconHelpers.GetWeeniePtr((int)info->ItemId);
                        ACCWeenieObject* aCCWeenieObject = (ACCWeenieObject*)ptrr;
                        aCCWeenieObject->SetWaitingState(0);
                    }
                }
            }
        }
        public void ShowInBar(bool show)
        {
            hud.ShowInBar = show;
        }
        private void OneTouchHealHotkey_Fired2(object sender, VirindiHotkeySystem.VHotkeyInfo.cEatableFiredEventArgs e)
        {
            try
            {
                VirindiHotkeySystem.VHotkeyInfo keyInfo = (VirindiHotkeySystem.VHotkeyInfo)sender;

                if (!CoreManager.Current.Actions.ChatState || keyInfo.AltState || keyInfo.ControlState)
                    oneTouchHeal.Heal();
            }
            catch (Exception ex) { PluginCore.Log(ex); }
        }

        unsafe private void Hud_OnRender(object sender, EventArgs e)
        {
            try
            {
                hovered = ImGui.IsWindowHovered();
                ImGui.Text($"Current Healing Skill: {oneTouchHeal.GetHealingSkill()}    Missing HP: {oneTouchHeal.MissingHP()}");

                if (ImGui.BeginTable(
                    "HealKitsTable",
                    7,
                    ImGuiTableFlags.Borders |
                    ImGuiTableFlags.RowBg |
                    ImGuiTableFlags.SizingStretchProp))
                {
                    // Headers
                    ImGui.TableSetupColumn("Actions");
                    ImGui.TableSetupColumn("Icon", ImGuiTableColumnFlags.WidthFixed, 40f);
                    ImGui.TableSetupColumn("Name");
                    ImGui.TableSetupColumn("Chance\r\nof\r\nSuccess");
                    ImGui.TableSetupColumn("Missing\r\nHP at\r\nChance");
                    ImGui.TableSetupColumn("Amount\r\nHealed");
                    ImGui.TableSetupColumn("Current\r\nHeal\r\nChance");
                    ImGui.TableHeadersRow();

                    for (int i = 0; i < OneTouchHeal.HealKits.Count; i++)
                    {
                        var kit = OneTouchHeal.HealKits[i];

                        ImGui.TableNextRow();

                        // Actions
                        ImGui.TableSetColumnIndex(0);

                        // Delete
                        if (ImGui.Button($"Delete##{i}"))
                        {
                            OneTouchHeal.HealKits.RemoveAt(i);

                            healKits.SetValue(OneTouchHeal.HealKits);
                            //Settings.Save();
                            // Don't increment i since the next item shifted into this index.
                            i--;
                            continue;
                        }

                        ImGui.SameLine();

                        // Move up
                        if (ImGui.ArrowButton($"up##{i}", ImGuiDir.Up) && i > 0)
                        {
                            (OneTouchHeal.HealKits[i], OneTouchHeal.HealKits[i - 1]) =
                                (OneTouchHeal.HealKits[i - 1], OneTouchHeal.HealKits[i]);

                            healKits.SetValue(OneTouchHeal.HealKits);
                            //Settings.Save();
                        }

                        ImGui.SameLine();

                        // Move down
                        if (ImGui.ArrowButton($"down##{i}", ImGuiDir.Down) &&
                            i < OneTouchHeal.HealKits.Count - 1)
                        {
                            (OneTouchHeal.HealKits[i], OneTouchHeal.HealKits[i + 1]) =
                                (OneTouchHeal.HealKits[i + 1], OneTouchHeal.HealKits[i]);

                            healKits.SetValue(OneTouchHeal.HealKits);
                            //Settings.Save();
                        }

                        // Icon
                        ImGui.TableSetColumnIndex(1);

                        ImGui.Image(
                            GetSingleIcon(kit.icon),
                            new Vector2(32, 32));

                        // Name
                        ImGui.TableSetColumnIndex(2);
                        ImGui.Text(kit.name);

                        if (!kit.food)
                        {
                            // Chance
                            ImGui.TableSetColumnIndex(3);

                            ImGui.SetNextItemWidth(100f);

                            if (ImGui.InputFloat(
                                $"##chance{i}",
                                ref kit.setChance,
                                0.1f,
                                1.0f,
                                "%.1f%%"))
                            {
                                if (kit.setChance < 0)
                                    kit.setChance = 0;

                                if (kit.setChance > 100)
                                    kit.setChance = 100;

                                //Settings.Save();
                            }



                            // Difficulty
                            ImGui.TableSetColumnIndex(4);
                            ImGui.Text(
                                kit.getMissingHPatChance().ToString("0")
                                + "hp");

                            // Amount
                            ImGui.TableSetColumnIndex(5);

                            var amt = kit.getAmountHealed();
                            ImGui.Text($"{amt.Item1} - {amt.Item2}");

                            // Heal Chance
                            ImGui.TableSetColumnIndex(6);
                            ImGui.Text(kit.healChance().ToString("0") + "%%");
                        }
                        else
                        {
                            // Food doesn't use chance/heal chance/etc.
                            ImGui.TableSetColumnIndex(5);
                            ImGui.Text(kit.bonusSkill.ToString("0") + "hp");
                        }
                    }

                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(1);
                    ImGui.Image(
                            GetSingleIcon(0x0F6E),
                            new Vector2(32, 32));
                    DragDropTarget();
                    ImGui.TableSetColumnIndex(2);
                    ImGui.Text("Drop new kit or food here");
                    DragDropTarget();
                    ImGui.EndTable();
                }


                if (oneTouchHealHotkey.KeyString == "")
                {
                    ImGui.Text("Bind a hotkey in Virindi Hotkey System");
                }
                if (!oneTouchHealHotkey.Enabled)
                {
                    ImGui.Text("Enable the hotkey in Virindi Hotkey System");
                }

                if (ImGui.Button("Save"))
                {
                    Settings.Save();
                }
                
                ImGui.SameLine();
                if (ImGui.Button("Help")) help = !help;
                if (help)
                {
                    ImGui.TextWrapped("When the hotkey is pressed this will check from the top of the list down. Each heal kits chance of success is calculated based on your missing HP and healing skill. If that calculated chance is higher than the setting in the \"Chance of Success\" column that heal kit will be used on yourself. If it isn't higher it will continue down the list. If it gets to a food or potion item it will check if its off cool-down and use it or skip it.\r\n\r\nYou should keep at least one kit at the bottom of the list set to 0%% chance of success so it will always try to use it if it gets to the bottom.\r\n\r\nMissing HP at Chance - Is the amount of hp missing that would calculate as the current chance of success\r\nAmount Healed - Is the amount that kit can heal for based on your current skill.\r\nCurrent Heal Chance - Is your chance of success right now with each kit.");
                }
                ImGui.SetWindowSize(new Vector2(-1, 0));
            }
            catch (Exception ex)
            {
                PluginCore.Log(ex);
            }
        }

        private unsafe void DragDropTarget()
        {
            if (ImGui.BeginDragDropTarget())
            {
                var payloaddd = ImGui.AcceptDragDropPayload("DND_ITEM_ID");

                var payloaddd2 = ImGui.AcceptDragDropPayload("ACDRAGDROP");

                if (payloaddd2.NativePtr != null)
                {
                    DragDropPayload drop = *(DragDropPayload*)payloaddd2.Data;
                    if (payloaddd2.IsDelivery())
                    {
                        var item = PluginCore.game.Character.Inventory.Where(x => x.Id == drop.ItemId).FirstOrDefault();
                        if (item != null && (item.ObjectClass == ObjectClass.Food || item.ObjectClass == ObjectClass.HealingKit))
                        {
                            oneTouchHeal.Add(item);
                            healKits.SetValue(OneTouchHeal.HealKits);
                            //Settings.Save();
                        }
                    }
                }

                if (payloaddd.NativePtr != null)
                {
                    uint drop = *(uint*)payloaddd.Data;

                    if (payloaddd.IsDelivery())
                    {
                        var item = PluginCore.game.Character.Inventory.Where(x => x.Id == drop).FirstOrDefault();
                        if (item != null && (item.ObjectClass == ObjectClass.Food || item.ObjectClass == ObjectClass.HealingKit))
                        {
                            oneTouchHeal.Add(item);
                            healKits.SetValue(OneTouchHeal.HealKits);
                            //Settings.Save();
                        }
                    }
                }
                ImGui.EndDragDropTarget();
            }
        }
        public IntPtr GetSingleIcon(uint iconID)
        {
            if (icons.ContainsKey(iconID))
            {
                return icons[iconID].TexturePtr;
            }
            else
            {
                var bmp = IconHelpers.GetBitmap(iconID);
                Color color = Color.FromArgb(0, 0, 0, 0);
                bmp = IconHelpers.ReplaceToColor(bmp, color);
                icons.Add(iconID, new ManagedTexture(bmp));
                return icons[iconID].TexturePtr;
            }
        }

        
        public void Dispose()
        {
            CoreManager.Current.WindowMessage -= Current_WindowMessage;
            oneTouchHealHotkey.Fired2 -= OneTouchHealHotkey_Fired2;
            VirindiHotkeySystem.VHotkeySystem.InstanceReal?.RemoveHotkey(oneTouchHealHotkey);
            oneTouchHeal.Dispose();
            healKits.SetValue(OneTouchHeal.HealKits);

            if (Settings != null)
            {
                if (Settings.NeedsSave)
                {
                    Settings.Save();
                }
            }
            hud.Dispose();
        }
    }
}