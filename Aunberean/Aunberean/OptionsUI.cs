using Decal.Adapter;
using Decal.Adapter.Wrappers;
using ImGuiNET;
using Microsoft.Extensions.Options;
using System;
using System.Numerics;
using UtilityBelt.Service;
using UtilityBelt.Service.Views;
using static Aunberean.WindowUI;
using Hud = UtilityBelt.Service.Views.Hud;

namespace Aunberean
{
    internal class OptionsUI : IDisposable
    {
        private readonly PluginCore _plugin;
        private readonly Hud hud;
        int windowPosition1x;
        int windowPosition1y;

        static bool ktEnable = true;
        static bool ktMark = true;
        static bool ktPoint = true;

        string TestText = "";
        public OptionsUI(PluginCore plugin)
        {
            _plugin = plugin;

            hud = UBService.Huds.CreateHud("Options for Aunberean");
            hud.ShowInBar = true;
            hud.OnRender += Hud_OnRender;
            hud.WindowSettings = ImGuiWindowFlags.AlwaysAutoResize;
            //hud.Visible = true;

            windowPosition1x = _plugin.windowPosition1.Value.x;
            windowPosition1y = _plugin.windowPosition1.Value.y;

            ktEnable = _plugin.ktEnable.Value;
            ktMark = _plugin.ktMark.Value;
            ktPoint = _plugin.ktPoint.Value;
        }

        private void Hud_OnRender(object sender, EventArgs e)
        {
            try
            {
                if (ImGui.BeginTabBar("Options"))
                {
                    if (ImGui.BeginTabItem("Vital bar"))
                    {
                        var enabled = _plugin.vitalBar.Value;
                        if (ImGui.Checkbox("Enabled", ref enabled))
                        {
                            _plugin.vitalBar.SetValue(enabled);
                            _plugin.vitalUI.setVisibility(enabled);
                        }

                        var simple = _plugin.simpleVitalBar.Value;
                        if (ImGui.Checkbox("Simple", ref simple))
                        {
                            _plugin.simpleVitalBar.SetValue(simple);
                        }

                        var side = _plugin.sideBySideStaminaMana.Value;
                        if (ImGui.Checkbox("Side by side", ref side))
                        {
                            _plugin.sideBySideStaminaMana.SetValue(side);
                        }

                        //show buffs
                        //show debuffs
                        ImGui.EndTabItem();
                    }
                    //if (ImGui.BeginTabItem("Window Position"))
                    //{

                        
                    //    if (ImGui.Button("Capture"))
                    //    {
                    //        var pos = _plugin.windowUI.GetWindow();
                    //        _plugin.windowPosition1.SetValue((pos.x, pos.y));
                    //    }
                    //    ImGui.SameLine();
                    //    if(ImGui.InputInt("X###X1", ref windowPosition1x))
                    //    {
                    //        _plugin.windowPosition1.SetValue((windowPosition1x, windowPosition1y));
                    //    }
                    //    ImGui.SameLine();
                    //    if(ImGui.InputInt("Y###Y1", ref windowPosition1y))
                    //    {
                    //        _plugin.windowPosition1.SetValue((windowPosition1x, windowPosition1y));
                    //    }

                    //    int x2 = _plugin.windowPosition2.Value.x;
                    //    int y2 = _plugin.windowPosition2.Value.y;
                    //    if (ImGui.Button("Capture"))
                    //    {
                    //        var pos = _plugin.windowUI.GetWindow();
                    //        _plugin.windowPosition1.SetValue((pos.x, pos.y));
                    //    }
                    //    ImGui.SameLine();
                    //    if (ImGui.InputInt("X", ref x2))
                    //    {
                    //        _plugin.windowPosition1.SetValue((x2, y2));
                    //    }
                    //    ImGui.SameLine();
                    //    if (ImGui.InputInt("Y", ref y2))
                    //    {
                    //        _plugin.windowPosition1.SetValue((x2, y2));
                    //    }

                    //    int x3 = _plugin.windowPosition3.Value.x;
                    //    int y3 = _plugin.windowPosition3.Value.y;
                    //    if (ImGui.Button("Capture"))
                    //    {
                    //        var pos = _plugin.windowUI.GetWindow();
                    //        _plugin.windowPosition1.SetValue((pos.x, pos.y));
                    //    }
                    //    ImGui.SameLine();
                    //    if (ImGui.InputInt("X", ref x3))
                    //    {
                    //        _plugin.windowPosition1.SetValue((x3, y3));
                    //    }
                    //    ImGui.SameLine();
                    //    if (ImGui.InputInt("Y", ref y3))
                    //    {
                    //        _plugin.windowPosition1.SetValue((x3, y3));
                    //    }

                    //    int x4 = _plugin.windowPosition4.Value.x;
                    //    int y4 = _plugin.windowPosition4.Value.y;
                    //    if (ImGui.Button("Capture"))
                    //    {
                    //        var pos = _plugin.windowUI.GetWindow();
                    //        _plugin.windowPosition1.SetValue((pos.x, pos.y));
                    //    }
                    //    ImGui.SameLine();
                    //    if (ImGui.InputInt("X", ref x4))
                    //    {
                    //        _plugin.windowPosition1.SetValue((x4, y4));
                    //    }
                    //    ImGui.SameLine();
                    //    if (ImGui.InputInt("Y", ref y4))
                    //    {
                    //        _plugin.windowPosition1.SetValue((x4, y4));
                    //    }

                    //    ImGui.EndTabItem();
                    //}

                    if (ImGui.BeginTabItem("Kill Task"))
                    {
                        //if (ImGui.Checkbox("Enable Kill Task Tracker", ref ktEnable))
                        //{
                        //    if (ktEnable)
                        //    {

                        //    } else
                        //    {

                        //    }
                        //}

                        if (ImGui.Checkbox("Mark Mobs", ref ktMark))
                        {
                            if (ktMark)
                            {
                                CoreManager.Current.Actions.AddChatText("Marking existing mobs", 1);
                                _plugin.ktui.addExistingShapes();

                            }
                            else
                            {
                                CoreManager.Current.Actions.AddChatText("Deleting all marks", 1);
                                _plugin.ktui.deleteAllShapes();
                            }
                            _plugin.ktMark.SetValue(ktMark);
                        }
                        if (ImGui.Checkbox("Point Mobs", ref ktPoint))
                        {
                            _plugin.ktPoint.SetValue(ktPoint);
                        }

                        //if (ImGui.Button("Mark existing"))
                        //{
                        //    _plugin.ktui.addExistingShapes();
                        //}
                        //if (ImGui.Button("Clear all markers"))
                        //{
                        //    _plugin.ktui.deleteAllShapes();
                        //}

                        //filter kill task message
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Chat Filters"))
                    {
                        var filterCloakOther = _plugin.filterCloakOther.Value;
                        if (ImGui.Checkbox("Cloak Other", ref filterCloakOther))
                        {
                            _plugin.filterCloakOther.SetValue(filterCloakOther);
                        }

                        var filterCloakSelf = _plugin.filterCloakSelf.Value;
                        if (ImGui.Checkbox("Cloak Self", ref filterCloakSelf))
                        {
                            _plugin.filterCloakSelf.SetValue(filterCloakSelf);
                        }

                        var filterAetheriaOther = _plugin.filterAetheriaOther.Value;
                        if (ImGui.Checkbox("Aetheria Other", ref filterAetheriaOther))
                        {
                            _plugin.filterAetheriaOther.SetValue(filterAetheriaOther);
                        }

                        var filterAetheriaSelf = _plugin.filterAetheriaSelf.Value;
                        if (ImGui.Checkbox("Aetheria Self", ref filterAetheriaSelf))
                        {
                            _plugin.filterAetheriaSelf.SetValue(filterAetheriaSelf);
                        }

                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Misc"))
                    {
                        var corpseTransparency = _plugin.corpseTransparency.Value;
                        if (ImGui.Checkbox("Corpse Transparency", ref corpseTransparency))
                        {
                            _plugin.corpseTransparency.SetValue(corpseTransparency);
                        }

                        if (corpseTransparency) {
                            float corpseTransparencyAmount = _plugin.corpseTransparencyAmount.Value;
                            if (ImGui.SliderFloat("###Corpse Transparency Amount", ref corpseTransparencyAmount, 0f, 1f))
                            {
                                _plugin.corpseTransparencyAmount.SetValue(corpseTransparencyAmount);
                            }
                        }
                        

                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();
                }

            }
            catch (Exception ex)
            {
                PluginCore.Log(ex);
            }
        }

        public void Dispose()
        {
            hud.Dispose();
        }
    }
}