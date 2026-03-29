using AcClient;
using ACE.DatLoader;
using ACE.DatLoader.Entity;
using ACE.DatLoader.FileTypes;
using ACE.Entity.Models;
using Decal.Adapter;
using Decal.Adapter.Wrappers;
using Decal.Filters;
using ImGuiNET;
using Microsoft.DirectX.Direct3D;
using ProtoBuf.Meta;
using ProtoBuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using UtilityBelt.Common.Enums;
using UtilityBelt.Common.Messages.Types;
using UtilityBelt.Scripting.Interop;
using UtilityBelt.Scripting.Lib;
using UtilityBelt.Service;
using UtilityBelt.Service.Lib.Settings;
using UtilityBelt.Service.Views;
using static System.Net.Mime.MediaTypeNames;
using Hud = UtilityBelt.Service.Views.Hud;
using Palette = ACE.DatLoader.FileTypes.Palette;
using SpellTable = Decal.Filters.SpellTable;
using Vector4 = System.Numerics.Vector4;

namespace Aunberean
{
    public class VitalUI : IDisposable
    {
        public readonly static Game Game = new();
        private readonly Hud hud;
        PluginCore _plugin;

        public Dictionary<int, ManagedTexture> icons;

        Vector2 size3232 = new Vector2(32, 32);

        ManagedTexture hpTexture;

        ManagedTexture healthstartTexture;
        ManagedTexture healthmiddleTexture;
        ManagedTexture healthendTexture;
        ManagedTexture healthbgstartTexture;
        ManagedTexture healthbgmiddleTexture;
        ManagedTexture healthbgendTexture;

        ManagedTexture staminastartTexture;
        ManagedTexture staminamiddleTexture;
        ManagedTexture staminaendTexture;
        ManagedTexture staminabgstartTexture;
        ManagedTexture staminabgmiddleTexture;
        ManagedTexture staminabgendTexture;

        ManagedTexture manastartTexture;
        ManagedTexture manamiddleTexture;
        ManagedTexture manaendTexture;
        ManagedTexture manabgstartTexture;
        ManagedTexture manabgmiddleTexture;
        ManagedTexture manabgendTexture;

        static ManagedTexture hashmarksTexture;
        static ManagedTexture greenAetheriaTexture;

        public static readonly List<uint> trackedSpells = new List<uint> {
            3204, // Blazing Heart
            5127, // Answer of Loyalty (Mana)
            5131, // Answer of Loyalty (Stam)
            5132, // Answer of Loyalty (Stam)
            5978, // Rare Armor Damage
            5192, // Rare Damage Reduction
            6170, // Life Mead
            5966, // Vigor Mead
            5122, // Call of Leadership V
            3531, 3533, 3862, 3864, 3530, 3863, //beers
            3869, // Incantation of the Black Book (Pages of Salt and Ash)
            5204, // Surge of Destruction
            5208, // Surge of Regen
            5206, // Surge of Protection
            5753, // Cloaked in Skill
            4280, // Deck of Hands
            4281, // Deck of Eyes
            //2347, // Concentration
            2348, // Brilliance
        };

        double enchantmentTime = 0;

        public VitalUI(PluginCore plugin)
        {
            _plugin = plugin;

            var assembly = Assembly.GetExecutingAssembly();

            string resourceName = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith("hashmarks.png", StringComparison.OrdinalIgnoreCase));
            if (resourceName == null) throw new FileNotFoundException("Embedded resource hashmarks.png not found.");

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                hashmarksTexture = new ManagedTexture(stream);
            }

            resourceName = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith("greenAetheria.png", StringComparison.OrdinalIgnoreCase));
            if (resourceName == null) throw new FileNotFoundException("Embedded resource greenAetheria.png not found.");

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                greenAetheriaTexture = new ManagedTexture(stream);
            }

            //debuff @castspell 3268
            hpTexture = new ManagedTexture((uint)0x1200);

            healthbgstartTexture = new ManagedTexture((uint)0x747e);
            healthbgmiddleTexture = new ManagedTexture((uint)0x747f);
            healthbgendTexture = new ManagedTexture((uint)0x7480);

            healthstartTexture = new ManagedTexture((uint)0x7481);
            healthmiddleTexture = new ManagedTexture((uint)0x7482);
            healthendTexture = new ManagedTexture((uint)0x7483);

            staminabgstartTexture = new ManagedTexture((uint)0x7484);
            staminabgmiddleTexture = new ManagedTexture((uint)0x7485);
            staminabgendTexture = new ManagedTexture((uint)0x7486);

            staminastartTexture = new ManagedTexture((uint)0x7487);
            staminamiddleTexture = new ManagedTexture((uint)0x7488);
            staminaendTexture = new ManagedTexture((uint)0x7489);

            manabgstartTexture = new ManagedTexture((uint)0x748a);
            manabgmiddleTexture = new ManagedTexture((uint)0x748b);
            manabgendTexture = new ManagedTexture((uint)0x748c);

            manastartTexture = new ManagedTexture((uint)0x748D);
            manamiddleTexture = new ManagedTexture((uint)0x748E);
            manaendTexture = new ManagedTexture((uint)0x748F);

            hud = UBService.Huds.CreateHud("Debuff");
            hud.DontDrawDefaultWindow = true;
            hud.WindowSettings = ImGuiWindowFlags.NoTitleBar
               | ImGuiWindowFlags.NoBackground;
            //| ImGuiWindowFlags.AlwaysAutoResize;

            icons = new Dictionary<int, ManagedTexture>();

            icons.Add(1, greenAetheriaTexture);
            hud.ShowInBar = false;

            hud.OnRender += Hud_OnRender;
            hud.OnShow += Hud_OnShow;
            hud.OnHide += Hud_OnHide;
            if (_plugin.vitalBar.Value) {
                hud.Visible = true;
            }
            
        }



        private void Hud_OnRender(object sender, EventArgs e)
        {
            try
            {
                
                var size = ImGui.GetContentRegionAvail();
                if (size.X <= 0 || size.Y <= 0)
                    return;
                
                var io = ImGui.GetIO();
                if (io.DisplaySize.X < 900 || io.DisplaySize.Y < 700)
                {
                    return; // skip frame or skip saving
                }
                hud.WindowSettings = ImGuiWindowFlags.NoTitleBar
                                    | ImGuiWindowFlags.NoBackground;
                if (!ImGui.GetIO().KeyCtrl)
                {
                    hud.WindowSettings |= ImGuiWindowFlags.NoInputs;
                }
                ImGui.SetNextWindowSizeConstraints(
                   new Vector2(350, 180),  // min size
                   new Vector2(1500, 500)   // max size
                );
                ImGui.SetNextWindowPos(new Vector2(_plugin.vitalPosX.Value, _plugin.vitalPosY.Value), ImGuiCond.FirstUseEver);
                ImGui.SetNextWindowSize(new Vector2(_plugin.vitalSizeX.Value, _plugin.vitalSizeY.Value), ImGuiCond.FirstUseEver);
                
                ImGui.Begin("Vitals", hud.WindowSettings);

                //var ween = Game.Character.Weenie;

                //UtilityBelt.Scripting.Interop.Skill value = null;
                //if (ween.Skills.TryGetValue(SkillId.Summoning, out value))
                //{
                //    ImGui.Text(value.Current.ToString());
                //}
                healthmiddleTexture.Texture.Device.SetSamplerState(0, SamplerStageStates.AddressU, 1);
                //middleTexture.Texture.Device.SetSamplerState(0, SamplerStageStates.AddressV, 1);
                
                SpellTable spellTable = ((FileService)CoreManager.Current.FileService).SpellTable;

                //var SpellTable = UBService.PortalDat.ReadFromDat<ACE.DatLoader.FileTypes.SpellTable>(0x0E00000E);
                //if (SpellTable.Spells.TryGetValue((uint)id, out SpellBase spell))
                //{
                //    //spell;
                //}

                var activeEnchantments = Game.Character.ActiveEnchantments();
                
                enchantmentTime = GetUnixTime();

                foreach (var enchantment in activeEnchantments.OrderBy(x=>x.SpellId))
                {
                    if (trackedSpells.Contains((uint)enchantment.SpellId))//||true)
                    {
                        Decal.Filters.Spell byId = spellTable.GetById((int)enchantment.SpellId);
                        if (byId != null)
                        {
                            //CoreManager.Current.Actions.AddChatText(byId.IconId.ToString(), 1);
                            var iconid = byId.IconId;
                            if (enchantment.SpellId == 5204) iconid = 100690955;// 0x6bf4;//dest
                            if (enchantment.SpellId == 5208) iconid = 100690956;// 0x6c08;//regen
                            if (enchantment.SpellId == 5206) iconid = 100690954;// 0x6c00;//prot
                            if (enchantment.SpellId == 5753) iconid = 0x7090;//cis
                            
                            var recivedat = GetUnixTime(enchantment.ClientReceivedAt);
                            var ExpiresAt = recivedat + enchantment.Duration + enchantment.StartTime;
                            var timeRemaning = enchantment.Duration <= 0 ? -1 : ExpiresAt - enchantmentTime;
                            drawBuff(iconid, (int)timeRemaning, 0);
                            if (ImGui.IsItemHovered())
                            {
                                ImGui.SetTooltip(byId.Name);
                            }
                            ImGui.SameLine();
                        }
                    }
                }

                var hp = GetVital(VitalId.Health);
                var stamina = GetVital(VitalId.Stamina);
                var mana = GetVital(VitalId.Mana);

                float barWidth = ImGui.GetWindowSize().X - 10;
                ImGui.SetCursorPos(new Vector2(0, 60));
                if (_plugin.simpleVitalBar.Value) {
                    DrawSimpleBar(hp.maxDisplay, (float)hp.current, (float)hp.max, _plugin.hpBarColor.Value, hp.divisor, barWidth);
                    ImGui.SetCursorPos(new Vector2(0, 76));

                    if (_plugin.sideBySideStaminaMana.Value) {
                        DrawSimpleBar(stamina.maxDisplay, (float)stamina.current, (float)stamina.max, _plugin.staminaBarColor.Value, stamina.divisor, (barWidth / 2) - 1);
                        ImGui.SetCursorPos(new Vector2((barWidth / 2), 76));
                        DrawSimpleBar(mana.maxDisplay, (float)mana.current, (float)mana.max, _plugin.manaBarColor.Value, mana.divisor, (barWidth/2)-1);
                        ImGui.Dummy(new Vector2(32, 48));
                        ImGui.SetCursorPos(new Vector2(8, 76 + 16 + 3));
                    } else
                    {
                        DrawSimpleBar(stamina.maxDisplay, (float)stamina.current, (float)stamina.max, _plugin.staminaBarColor.Value, stamina.divisor, barWidth);
                        ImGui.SetCursorPos(new Vector2(0, 76 + 16));
                        DrawSimpleBar(mana.maxDisplay, (float)mana.current, (float)mana.max, _plugin.manaBarColor.Value, mana.divisor, barWidth);
                        ImGui.Dummy(new Vector2(32, 48));
                        ImGui.SetCursorPos(new Vector2(8, 76 + 16 + 16 + 3));
                    }
                }
                else
                {
                    drawHpBar(hp.current, hp.max, hp.divisor, hp.maxDisplay,
                            healthbgstartTexture.TexturePtr, healthbgmiddleTexture.TexturePtr, healthbgendTexture.TexturePtr,
                            healthstartTexture.TexturePtr, healthmiddleTexture.TexturePtr, healthendTexture.TexturePtr, barWidth);
                    ImGui.SetCursorPos(new Vector2(0, 76));
                    if (_plugin.sideBySideStaminaMana.Value)
                    {
                        drawHpBar(stamina.current, stamina.max, stamina.divisor, stamina.maxDisplay,
                                staminabgstartTexture.TexturePtr, staminabgmiddleTexture.TexturePtr, staminabgendTexture.TexturePtr,
                                staminastartTexture.TexturePtr, staminamiddleTexture.TexturePtr, staminaendTexture.TexturePtr, barWidth/2);
                        ImGui.SetCursorPos(new Vector2(barWidth / 2, 76));
                        drawHpBar(mana.current, mana.max, mana.divisor, mana.maxDisplay,
                                    manabgstartTexture.TexturePtr, manabgmiddleTexture.TexturePtr, manabgendTexture.TexturePtr,
                                    manastartTexture.TexturePtr, manamiddleTexture.TexturePtr, manaendTexture.TexturePtr, barWidth/2);
                    } else
                    {
                        drawHpBar(stamina.current, stamina.max, stamina.divisor, stamina.maxDisplay,
                            staminabgstartTexture.TexturePtr, staminabgmiddleTexture.TexturePtr, staminabgendTexture.TexturePtr,
                            staminastartTexture.TexturePtr, staminamiddleTexture.TexturePtr, staminaendTexture.TexturePtr, barWidth);
                        ImGui.SetCursorPos(new Vector2(0, 76 + 16));
                        drawHpBar(mana.current, mana.max, mana.divisor, mana.maxDisplay,
                                    manabgstartTexture.TexturePtr, manabgmiddleTexture.TexturePtr, manabgendTexture.TexturePtr,
                                    manastartTexture.TexturePtr, manamiddleTexture.TexturePtr, manaendTexture.TexturePtr, barWidth);
                    }
                    
                }


                foreach (var enchantment in activeEnchantments.OrderBy(x => x.SpellId))
                {
                    if (enchantment.Duration == -1) continue;
                    if (enchantment.SpellId <= 0) continue;
                    Decal.Filters.Spell byId = spellTable.GetById((int)enchantment.SpellId);
                    if (byId == null) continue;
                    if (!byId.IsDebuff) continue;

                    var recivedat = GetUnixTime(enchantment.ClientReceivedAt);
                    var ExpiresAt = recivedat + enchantment.Duration + enchantment.StartTime;
                    var timeRemaning = enchantment.Duration <= 0 ? -1 : ExpiresAt - enchantmentTime;
                    
                    drawBuff(byId.IconId, (int)timeRemaning, (int)enchantment.Power, Color.Red);
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip(byId.Name);
                    }

                    ImGui.SameLine();
                }

                var wpos = ImGui.GetWindowPos();
                var wsize = ImGui.GetWindowSize();
                _plugin.vitalPosX.SetValue(wpos.X);
                _plugin.vitalPosY.SetValue(wpos.Y);
                _plugin.vitalSizeX.SetValue(wsize.X);
                _plugin.vitalSizeY.SetValue(wsize.Y);

                ImGui.End();
            }
            catch (Exception ex)
            {
                ImGui.End();
                PluginCore.Log(ex);
            }
        }
        private unsafe void Hud_OnHide(object sender, EventArgs e)
        {
            var playerSystem = CPlayerSystem.GetPlayerSystem();
            if (playerSystem == null) return;
            
            if (playerSystem->playerModule.PlayerModule.GetOption(PlayerOption.SideBySideVitals_PlayerOption) == 1)
            {
                CoreManager.Current.Actions.AddChatText("was side side", 1);
                var vit = CoreManager.Current.Actions.UIElementLookup((Decal.Adapter.Wrappers.UIElementType)0x100006D5);
                if (vit != null)
                {
                    UIElement* ptr = (UIElement*)vit;
                    ptr->SetVisible((byte)(1));
                }
            }
            else
            {
                var vit = CoreManager.Current.Actions.UIElementLookup(Decal.Adapter.Wrappers.UIElementType.Vitals);
                if (vit != null)
                {
                    UIElement* ptr = (UIElement*)vit;
                    ptr->SetVisible((byte)(1));
                }
            }
        }

        private unsafe void Hud_OnShow(object sender, EventArgs e)
        {
            if (!_plugin.vitalBar.Value) return;
            var playerSystem = CPlayerSystem.GetPlayerSystem();
            if (playerSystem == null) return;
            if (playerSystem->playerModule.PlayerModule.GetOption(PlayerOption.SideBySideVitals_PlayerOption) == 1)
            {
                CoreManager.Current.Actions.AddChatText("was side side", 1);
                var vit = CoreManager.Current.Actions.UIElementLookup((Decal.Adapter.Wrappers.UIElementType)0x100006D5);
                if (vit != null)
                {
                    UIElement* ptr = (UIElement*)vit;
                    ptr->SetVisible((byte)(0));
                    //ptr->SetVisible((byte)(ptr->IsVisible() == 1 ? 0 : 1));
                }
            }
            else
            {
                var vit = CoreManager.Current.Actions.UIElementLookup(Decal.Adapter.Wrappers.UIElementType.Vitals);
                if (vit != null)
                {
                    UIElement* ptr = (UIElement*)vit;
                    ptr->SetVisible((byte)(0));
                    //ptr->SetVisible((byte)(ptr->IsVisible() == 1 ? 0 : 1));
                }
            }
        }
        private static readonly DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        public static double GetUnixTime()
        {
            return GetUnixTime(DateTime.UtcNow);
        }

        public static double GetUnixTime(DateTime dateTime)
        {
            TimeSpan span = (dateTime - unixEpoch);

            return span.TotalSeconds;
        }

        public (int current,int max,double divisor, string maxDisplay) GetVital(VitalId type)
        {
            var max = (double)Game.Character.Weenie.Vitals[type].Max;
            var maxDisplay = max;
            var current = Game.Character.Weenie.Vitals[type].Current;
            var multi = GetVitalNegativeMultiplierModifier(type);
            var sub = GetVitalNegativeAdditiveModifier(type);

            max -= sub;
            max /= multi;

            var divisor = maxDisplay / max;

            var maxOut = (int)Math.Round(max);

            return (current, maxOut, divisor, maxDisplay.ToString());
        }
        public float GetVitalNegativeMultiplierModifier(VitalId type)
        {
            float num = 1f;
            foreach (UtilityBelt.Scripting.Interop.Enchantment item in from e in Game.Character.GetActiveEnchantments(type)
                                                                       where (e.Flags & EnchantmentFlags.Multiplicative) != 0
                                                                       && e.StatValue < 1.0f
                                                                       select e)
            {
                num *= item.StatValue;
            }

            return num;
        }

        public int GetVitalNegativeAdditiveModifier(VitalId type)
        {
            return (from e in Game.Character.GetActiveEnchantments(type)
                    where (e.Flags & EnchantmentFlags.Additive) != 0
                    && e.StatValue < 0.0f
                    select e).Sum((UtilityBelt.Scripting.Interop.Enchantment e) => (int)e.StatValue);
        }


        public void drawBuff(int IconId, int TimeRemaining,int Difficulty, Color replaceColor = default(Color))
        {
            if (IconId < 100663296)
            {
                IconId += 100663296;
            }
            ImGui.BeginGroup();
            var curspos = ImGui.GetCursorPos();

            if (Difficulty != 0)
            {
                var bubble = SpellBubble(LevelFromSpellDifficulty(Difficulty));

                if (icons.ContainsKey(bubble))
                {
                    ImGui.Image(icons[bubble].TexturePtr, size3232);
                }
                else
                {
                    icons.Add(bubble, new ManagedTexture((uint)bubble));
                    ImGui.Image(icons[bubble].TexturePtr, size3232);
                }
                ImGui.SetCursorPos(curspos);
            }
            
            if (icons.ContainsKey(IconId))
            {

                ImGui.Image(icons[IconId].TexturePtr, size3232);

            }
            else
            {
                var tex = UBService.PortalDat.ReadFromDat<ACE.DatLoader.FileTypes.Texture>((uint)IconId);
                if (tex != null)
                {
                    Bitmap bmp = GetBitmap(tex);
                    ReplaceWhite(bmp, replaceColor);

                    icons.Add(IconId, new ManagedTexture(bmp));
                    ImGui.Image(icons[IconId].TexturePtr, size3232);
                }
            }
            
            TimeSpan t = TimeSpan.FromSeconds(TimeRemaining);
            string text = $"{(int)t.TotalMinutes}:{t.Seconds:D2}";
            var textSize = ImGui.CalcTextSize(text);

            // Get current X so we can offset within the group
            float cursorX = ImGui.GetCursorPosX();

            // Center text within the icon width
            float offset = (size3232.X - textSize.X) * 0.5f;
            if (offset > 0)
            {
                ImGui.SetCursorPosX(cursorX + offset);
            }
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 2); // tweak spacing
            var pos = ImGui.GetCursorPos();

            // Shadow color
            ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(0f, 0f, 0f, 1f));

            // Offsets (outline)
            Vector2[] offsets = {
                        new Vector2(1, 0),
                        new Vector2(-1, 0),
                        new Vector2(0, 1),
                        new Vector2(0, -1)
                    };

            foreach (var offseta in offsets)
            {
                ImGui.SetCursorPos(pos + offseta);
                ImGui.Text(text);
            }

            ImGui.PopStyleColor(1);

            ImGui.SetCursorPos(pos);
            ImGui.Text(text);
            ImGui.EndGroup();
        }
        private void drawHpBar(double stat, double statmax, double reduction, string statmaxdisplay, IntPtr bgStart, IntPtr bgMiddle, IntPtr bgEnd, IntPtr start, IntPtr middle, IntPtr end, float width)
        {

            var statf = (float)(stat / statmax);

            //var windowsize = ImGui.GetWindowSize();

            Vector2 pos = ImGui.GetCursorScreenPos();
            ThreeSliceBar(
                1.0f, // always full width
                new Vector2(width, 16),
                bgStart,
                bgMiddle,
                bgEnd,
                12f,
                12f,
                10f
            );
            ImGui.SetCursorScreenPos(pos);
            //ImGui.Dummy(new Vector2(32, 32)) ;
            if (reduction != 1)
            {
                TexturedProgressBar((float)reduction,
                new Vector2(width, 16),
                hashmarksTexture.TexturePtr
                );
                ImGui.SetCursorScreenPos(pos);
            }
            
            ThreeSliceBar_Clipped(
                statf,
                new Vector2(width, 16),
                start,
                middle,
                end,
                12f,   // start cap width
                12f,   // end cap width
                100   // tiling density
            );
            ImGui.SetCursorScreenPos(pos);

            string text2 = stat + "/" + statmaxdisplay;
            var textSize2 = ImGui.CalcTextSize(text2);

            // Get current X so we can offset within the group
            float cursorX2 = ImGui.GetCursorPosX();

            // Center text within the icon width
            float offset2 = (width - textSize2.X) * 0.5f;
            if (offset2 > 0)
            {
                ImGui.SetCursorPosX(cursorX2 + offset2);
            }
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 1); // tweak spacing

            //var pos3 = ImGui.GetCursorPos();

            //// Shadow color
            //ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(0f, 0f, 0f, 1f));

            //// Offsets (outline)
            //Vector2[] offsets = {
            //            new Vector2(1, 0),
            //            new Vector2(-1, 0),
            //            new Vector2(0, 1),
            //            new Vector2(0, -1)
            //        };

            //foreach (var offseta in offsets)
            //{
            //    ImGui.SetCursorPos(pos3 + offseta);
            //    ImGui.Text(text2);
            //}

            //ImGui.PopStyleColor(1);
            //ImGui.SetCursorScreenPos(pos3);
            ImGui.Text(text2);
        }

        private void drawStaminaBar(double stat, double statmax)
        {
            var statf = (float)(stat / statmax);

            var windowsize = ImGui.GetWindowSize();
            ImGui.SetCursorPosX(0);
            Vector2 pos = ImGui.GetCursorScreenPos();
            ThreeSliceBar(
                1.0f, // always full width
                new Vector2((windowsize.X - 10) / 2, 16),
                staminabgstartTexture.TexturePtr,
                staminabgmiddleTexture.TexturePtr,
                staminabgendTexture.TexturePtr,
                12f,
                12f,
                10f
            );
            ImGui.SetCursorScreenPos(pos);

            ThreeSliceBar_Clipped(
                statf,
                new Vector2((windowsize.X - 10)/2, 16),
                staminastartTexture.TexturePtr,
                staminamiddleTexture.TexturePtr,
                staminaendTexture.TexturePtr,
                12f,   // start cap width
                12f,   // end cap width
                100   // tiling density
            );
            ImGui.SetCursorScreenPos(pos);

            string text2 = stat + "/" + statmax;
            var textSize2 = ImGui.CalcTextSize(text2);

            // Get current X so we can offset within the group
            float cursorX2 = ImGui.GetCursorPosX();

            // Center text within the icon width
            float offset2 = ((windowsize.X - 10)/2 - textSize2.X) * 0.5f;
            if (offset2 > 0)
            {
                ImGui.SetCursorPosX(cursorX2 + offset2);
            }
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 1); // tweak spacing
            ImGui.Text(text2);
        }

        private void drawManaBar(double stat, double statmax)
        {
            var statf = (float)(stat / statmax);

            var windowsize = ImGui.GetWindowSize();

            Vector2 pos = ImGui.GetCursorScreenPos();
            ThreeSliceBar(
                1.0f, // always full width
                new Vector2((windowsize.X - 10) / 2, 16),
                manabgstartTexture.TexturePtr,
                manabgmiddleTexture.TexturePtr,
                manabgendTexture.TexturePtr,
                12f,
                12f,
                10f
            );
            ImGui.SetCursorScreenPos(pos);

            ThreeSliceBar_Clipped(
                statf,
                new Vector2((windowsize.X - 10) / 2, 16),
                manastartTexture.TexturePtr,
                manamiddleTexture.TexturePtr,
                manaendTexture.TexturePtr,
                12f,   // start cap width
                12f,   // end cap width
                100   // tiling density
            );
            ImGui.SetCursorScreenPos(pos);

            string text2 = stat + "/" + statmax;
            var textSize2 = ImGui.CalcTextSize(text2);

            // Get current X so we can offset within the group
            float cursorX2 = ImGui.GetCursorPosX();

            // Center text within the icon width
            float offset2 = ((windowsize.X - 10) / 2 - textSize2.X) * 0.5f;
            if (offset2 > 0)
            {
                ImGui.SetCursorPosX(cursorX2 + offset2);
            }
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 1); // tweak spacing
            ImGui.Text(text2);
        }
        public static void TexturedProgressBar(
        float fraction,
        Vector2 size,
        IntPtr textureId)
        {
            var drawList = ImGui.GetWindowDrawList();
            var pos = ImGui.GetCursorScreenPos();

            Vector2 end = new Vector2(pos.X + size.X, pos.Y + size.Y);

            Vector2 fillStart = new Vector2(pos.X + size.X * fraction, pos.Y);

            float pixelsToFill = (float)Math.Floor(size.X * (1 - fraction));
            float startX = (float)Math.Floor(pos.X + size.X * fraction);

            Vector2 cursor = new Vector2(startX, pos.Y);
            Vector2 endY = new Vector2(0, size.Y);

            int fullTiles = (int)(pixelsToFill / 100);
            float remainder = (float)Math.Floor(pixelsToFill % 100);

            // full tiles
            for (int i = 0; i < fullTiles; i++)
            {
                drawList.AddImage(
                    textureId,
                    cursor,
                    new Vector2(cursor.X + 100, cursor.Y + size.Y),
                    new Vector2(0, 0),
                    new Vector2(1, 1)
                );

                cursor.X += 100;
            }

            // remainder
            if (remainder >= 2.0f)
            {
                float u = remainder / 100.0f;

                drawList.AddImage(
                    textureId,
                    cursor,
                    new Vector2(cursor.X + remainder, cursor.Y + size.Y),
                    new Vector2(0, 0),
                    new Vector2(u, 1)
                );
            }

            ImGui.Dummy(size);
        }
        public static void ThreeSliceBar_Clipped(
            float fraction,
            Vector2 size,
            IntPtr startTex,
            IntPtr middleTex,
            IntPtr endTex,
            float startWidth,
            float endWidth,
            float middleTexWidth,
            uint tintColor = 0xffffffff
)
        {
            var drawList = ImGui.GetWindowDrawList();
            var pos = ImGui.GetCursorScreenPos();

            if (float.IsNaN(fraction))
                fraction = 0.0f;

            float totalWidth = size.X;
            float height = size.Y;

            if (fraction < 0.0f) fraction = 0.0f;
            if (fraction > 1.0f) fraction = 1.0f;
            float filledWidth = totalWidth * fraction;

            if (filledWidth <= 0.0f || float.IsNaN(filledWidth) || float.IsInfinity(filledWidth))
                return;

            if (height <= 0.0f || float.IsNaN(height) || float.IsInfinity(height))
                return;

            ImGui.Dummy(size);

            if (filledWidth <= 0.0f)
                return;

            // --- CLIP ---
            Vector2 clipMin = pos;
            Vector2 clipMax = new Vector2(pos.X + filledWidth, pos.Y + height);

            // Ensure valid rect
            if (clipMax.X < clipMin.X) clipMax.X = clipMin.X;
            if (clipMax.Y < clipMin.Y) clipMax.Y = clipMin.Y;

            drawList.PushClipRect(clipMin, clipMax, true);

            float x = pos.X;
            float y = pos.Y;

            // --- START ---
            if (startWidth > 0)
            {
                drawList.AddImage(
                    startTex,
                    new Vector2(x, y),
                    new Vector2(x + startWidth, y + height),
                    new Vector2(0, 0),
                    new Vector2(1, 1),
                    tintColor
                );

                x += startWidth;
            }

            // --- MIDDLE (AUTO-TILED) ---
            float middleWidth = Math.Max(0, totalWidth - startWidth - endWidth);

            if (middleWidth > 0)
            {
                float tiledU = middleWidth / middleTexWidth;

                drawList.AddImage(
                    middleTex,
                    new Vector2(x, y),
                    new Vector2(x + middleWidth, y + height),
                    new Vector2(0, 0),
                    new Vector2(tiledU, 1),
                    tintColor
                );

                x += middleWidth;
            }

            // --- END ---
            if (endWidth > 0)
            {
                drawList.AddImage(
                    endTex,
                    new Vector2(x, y),
                    new Vector2(x + endWidth, y + height),
                    new Vector2(0, 0),
                    new Vector2(1, 1),
                    tintColor
                );
            }

            drawList.PopClipRect();
        }
        public static void ThreeSliceBar(
        float fraction,
        Vector2 size,
        IntPtr startTex,
        IntPtr middleTex,
        IntPtr endTex,
        float startWidth,
        float endWidth,
        float tileFactor,
        float rounding = 0f)
        {
            var drawList = ImGui.GetWindowDrawList();
            var pos = ImGui.GetCursorScreenPos();

            float totalWidth = size.X;
            float filledWidth = totalWidth * fraction;

            float x = pos.X;
            float y = pos.Y;
            float height = size.Y;

            // Clamp so we don't overflow
            float remaining = filledWidth;

            Vector4 tint = fraction < 0.25f
                            ? new Vector4(1f, 0.2f, 0.2f, 1f)
                            : new Vector4(1f, 1f, 1f, 1f);

            uint tintColor = ImGui.GetColorU32(tint);

            tintColor = 0xffFFffff;


            


            // --- START SEGMENT ---
            if (remaining > 0)
            {
                float w = Math.Min(startWidth, remaining);

                drawList.AddImage(
                    startTex,
                    new Vector2(x, y),
                    new Vector2(x + w, y + height),
                    new Vector2(0, 0),
                    new Vector2(w / startWidth, 1), // partial UV if clipped
                    tintColor
                );

                x += w;
                remaining -= w;
            }

            // --- MIDDLE (TILED) ---
            if (remaining > 0)
            {
                float middleWidth = Math.Max(0, remaining - endWidth);

                if (middleWidth > 0)
                {
                    float tiledU = (middleWidth / totalWidth) * tileFactor;

                    drawList.AddImage(
                        middleTex,
                        new Vector2(x, y),
                        new Vector2(x + middleWidth, y + height),
                        new Vector2(0, 0),
                        new Vector2(tiledU, 1),
                        tintColor
                    );

                    x += middleWidth;
                    remaining -= middleWidth;
                }
            }

            // --- END SEGMENT ---
            if (remaining > 0)
            {
                float w = Math.Min(endWidth, remaining);

                drawList.AddImage(
                    endTex,
                    new Vector2(x, y),
                    new Vector2(x + w, y + height),
                    new Vector2(0, 0),
                    new Vector2(w / endWidth, 1),
                    tintColor
                );
            }

            ImGui.Dummy(size);
        }
        public static void DrawSimpleBar(string label, float current, float max, uint barColor, double reduction, float width)
        {
            var drawList = ImGui.GetWindowDrawList();
            Vector2 pos = ImGui.GetCursorScreenPos();

            float height = 15.0f;
            Vector2 size = new Vector2(width, height);

            // Register the item in ImGui layout
            ImGui.Dummy(size);

            // 1. Draw Background (Dark Recessed Box)
            uint bgColor = ImGui.GetColorU32(new Vector4(0.08f, 0.08f, 0.08f, 1.0f)); // Near black
            drawList.AddRectFilled(pos, pos + size, bgColor);

            ImGui.SetCursorScreenPos(pos);
            //ImGui.Dummy(new Vector2(ImGui.GetWindowSize().X - 10, 16));
            if (reduction != 1)
            {
                TexturedProgressBar((float)reduction,
                new Vector2(width, 16),
                hashmarksTexture.TexturePtr
                );
                ImGui.SetCursorScreenPos(pos);
                ImGui.Dummy(new Vector2(0, 0));
            }

            // 2. Draw Fill Bar
            float fraction = current / max;
            if (fraction < 0.0f) fraction = 0.0f;
            if (fraction > 1.0f) fraction = 1.0f;

            if (fraction > 0.0f)
            {
                Vector2 fillMax = new Vector2(pos.X + (width * fraction), pos.Y + height);
                drawList.AddRectFilled(pos, fillMax, barColor);
            }

            // 3. Draw AC-style "Inner Shadow" Bevel
            // Top and Left (Pure Black)
            drawList.AddLine(pos, new Vector2(pos.X + width, pos.Y), 0xFF000000);
            drawList.AddLine(pos, new Vector2(pos.X, pos.Y + height), 0xFF000000);

            // Bottom and Right (Grey highlight for the 3D 'recessed' look)
            uint highlight = ImGui.GetColorU32(new Vector4(0.35f, 0.35f, 0.35f, 1.0f));
            drawList.AddLine(new Vector2(pos.X, pos.Y + height), pos + size, highlight);
            drawList.AddLine(new Vector2(pos.X + width, pos.Y), pos + size, highlight);

            // 4. Centered Text (White, pixel-style)
            string text = $"{(int)current} / {label}";
            Vector2 textSize = ImGui.CalcTextSize(text);
            Vector2 textPos = pos + (size - textSize) * 0.5f;


            // Offsets (outline)
            Vector2[] offsets = {
                        new Vector2(1, 0),
                        new Vector2(-1, 0),
                        new Vector2(0, 1),
                        new Vector2(0, -1)
                    };
            
            foreach (var offseta in offsets)
            {
                drawList.AddText(textPos + offseta, 0xFF000000, text);
            }


            drawList.AddText(textPos, 0xFFFFFFFF, text);
            
        }
        void ReplaceWhite(Bitmap bmp, Color newColor)
        {
            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    Color pixel = bmp.GetPixel(x, y);

                    if (pixel.R > 240 && pixel.G > 240 && pixel.B > 240)
                    {
                        bmp.SetPixel(x, y, Color.FromArgb(pixel.A, newColor));
                    }
                }
            }
        }

        public static int LevelFromSpellDifficulty(int difficulty)
        {
            if (difficulty > 390)
            {
                return 8;
            }
            if (difficulty > 290)
            {
                return 7;
            }
            if (difficulty > 240)
            {
                return 6;
            }
            if (difficulty > 190)
            {
                return 5;
            }
            if (difficulty > 140)
            {
                return 4;
            }
            if (difficulty > 90)
            {
                return 3;
            }
            if (difficulty > 40)
            {
                return 2;
            }
            if (difficulty > 1)
            {
                return 1;
            }
            return 0;
        }

        public static int SpellBubble(int level)
        {
            switch (level)
            {
                case 1:
                    return 5108;
                case 2:
                    return 5109;
                case 3:
                    return 5110;
                case 4:
                    return 5111;
                case 5:
                    return 5112;
                case 6:
                    return 5113;
                case 7:
                    return 8035;
                case 8:
                    return 26534;
                default:
                    return 5110;
            }
        }

        private Bitmap GetBitmap(ACE.DatLoader.FileTypes.Texture texture)
        {
            Bitmap bitmap = new Bitmap(texture.Width, texture.Height);
            List<int> imageColorArray = texture.GetImageColorArray();
            switch (texture.Format)
            {
                case SurfacePixelFormat.PFID_R8G8B8:
                case SurfacePixelFormat.PFID_CUSTOM_LSCAPE_R8G8B8:
                    {
                        for (int num7 = 0; num7 < texture.Height; num7++)
                        {
                            for (int num8 = 0; num8 < texture.Width; num8++)
                            {
                                int index4 = num7 * texture.Width + num8;
                                int red6 = (imageColorArray[index4] & 0xFF0000) >> 16;
                                int green6 = (imageColorArray[index4] & 0xFF00) >> 8;
                                int blue6 = imageColorArray[index4] & 0xFF;
                                bitmap.SetPixel(num8, num7, Color.FromArgb(red6, green6, blue6));
                            }
                        }

                        break;
                    }
                case SurfacePixelFormat.PFID_A8R8G8B8:
                    {
                        for (int num3 = 0; num3 < texture.Height; num3++)
                        {
                            for (int num4 = 0; num4 < texture.Width; num4++)
                            {
                                int index2 = num3 * texture.Width + num4;
                                int alpha3 = (int)((imageColorArray[index2] & 0xFF000000u) >> 24);
                                int red4 = (imageColorArray[index2] & 0xFF0000) >> 16;
                                int green4 = (imageColorArray[index2] & 0xFF00) >> 8;
                                int blue4 = imageColorArray[index2] & 0xFF;
                                bitmap.SetPixel(num4, num3, Color.FromArgb(alpha3, red4, green4, blue4));
                            }
                        }

                        break;
                    }
                case SurfacePixelFormat.PFID_P8:
                case SurfacePixelFormat.PFID_INDEX16:
                    {
                        Palette palette = UBService.PortalDat.ReadFromDat<Palette>(texture.DefaultPaletteId.Value);
                        if (texture.CustomPaletteColors.Count > 0)
                        {
                            foreach (KeyValuePair<int, uint> customPaletteColor in texture.CustomPaletteColors)
                            {
                                if (customPaletteColor.Key <= palette.Colors.Count)
                                {
                                    palette.Colors[customPaletteColor.Key] = customPaletteColor.Value;
                                }
                            }
                        }

                        for (int k = 0; k < texture.Height; k++)
                        {
                            for (int l = 0; l < texture.Width; l++)
                            {
                                int index = k * texture.Width + l;
                                int alpha2 = (int)((palette.Colors[imageColorArray[index]] & 0xFF000000u) >> 24);
                                int red2 = (int)(palette.Colors[imageColorArray[index]] & 0xFF0000) >> 16;
                                int green2 = (int)(palette.Colors[imageColorArray[index]] & 0xFF00) >> 8;
                                int blue2 = (int)(palette.Colors[imageColorArray[index]] & 0xFF);
                                bitmap.SetPixel(l, k, Color.FromArgb(alpha2, red2, green2, blue2));
                            }
                        }

                        break;
                    }
                case SurfacePixelFormat.PFID_A8:
                case SurfacePixelFormat.PFID_CUSTOM_LSCAPE_ALPHA:
                    {
                        for (int num5 = 0; num5 < texture.Height; num5++)
                        {
                            for (int num6 = 0; num6 < texture.Width; num6++)
                            {
                                int index3 = num5 * texture.Width + num6;
                                int red5 = imageColorArray[index3];
                                int green5 = imageColorArray[index3];
                                int blue5 = imageColorArray[index3];
                                bitmap.SetPixel(num6, num5, Color.FromArgb(red5, green5, blue5));
                            }
                        }

                        break;
                    }
                case SurfacePixelFormat.PFID_R5G6B5:
                    {
                        for (int m = 0; m < texture.Height; m++)
                        {
                            for (int n = 0; n < texture.Width; n++)
                            {
                                int num2 = 3 * (m * texture.Width + n);
                                int red3 = imageColorArray[num2];
                                int green3 = imageColorArray[num2 + 1];
                                int blue3 = imageColorArray[num2 + 2];
                                bitmap.SetPixel(n, m, Color.FromArgb(red3, green3, blue3));
                            }
                        }

                        break;
                    }
                case SurfacePixelFormat.PFID_A4R4G4B4:
                    {
                        for (int i = 0; i < texture.Height; i++)
                        {
                            for (int j = 0; j < texture.Width; j++)
                            {
                                int num = 4 * (i * texture.Width + j);
                                int alpha = imageColorArray[num];
                                int red = imageColorArray[num + 1];
                                int green = imageColorArray[num + 2];
                                int blue = imageColorArray[num + 3];
                                bitmap.SetPixel(j, i, Color.FromArgb(alpha, red, green, blue));
                            }
                        }

                        break;
                    }
            }

            return bitmap;
        }

        public void setVisibility(bool visibility)
        { hud.Visible = visibility; }
        public void Dispose()
        {
            
            hud.Dispose();

            foreach (var icon in icons)
            {
                icon.Value.Dispose();
            }

        }
    }
}