using AcClient;
using ACE.DatLoader.FileTypes;
using ACE.Entity;
using Decal.Adapter;
using Decal.Adapter.Wrappers;
using Decal.Filters;
using ImGuiNET;
using Microsoft.DirectX.Direct3D;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using UtilityBelt.Common.Enums;
using UtilityBelt.Common.Messages.Types;
using UtilityBelt.Scripting.Enums;
using UtilityBelt.Service;
using UtilityBelt.Service.Lib.ACClientModule;
using UtilityBelt.Service.Lib.Settings;
using UtilityBelt.Service.Views;
using static System.Net.Mime.MediaTypeNames;
using Hud = UtilityBelt.Service.Views.Hud;
using SpellTable = Decal.Filters.SpellTable;
using Vector3 = System.Numerics.Vector3;

namespace Aunberean
{
    public class KillTaskUI : IDisposable
    {
        private readonly PluginCore _plugin;

        private readonly Hud hud;
        private bool windowIsOpen;

        ManagedTexture iconRedX = new ManagedTexture(0x11F8);
        ManagedTexture iconGreenCircle = new ManagedTexture(0x11F9);
        ManagedTexture iconGreenPlus;
        ManagedTexture iconGreenArrow = new ManagedTexture(0x11F7);
        ManagedTexture pointerArrow;
        
        private Dictionary<int, (string Name, D3DObj D3DObj)> Shapes;

        Vector2 siz32 = new Vector2(32, 32);
        Vector2 siz16 = new Vector2(16, 16);

        static bool markMobs = true;
        static bool pointMobs = true;
        static bool enabled = false;
        //You have killed 1 Panumbris Shadows! You must kill 25 to complete your task.
        // You have killed 50 Drudge Raveners! Your task is complete!
        //You've killed 5 out of 25 Shadows.

        string pattern = @"^You have killed\s+(\d+)\s+(.+?)!\s+You must kill\s+(\d+)\s+to complete your task\.";
        string donePattern = @"^You have killed\s+(\d+)\s+(.+?)!\s+Your task is complete!";
        string midPattern = @"^You\'ve killed\s+(\d+)\s+out of\s+(\d+)\s+(.+?)\.";
        string vrPortal = @"^Viridian Portal gives you\s+(\d+)\s+Infused Amber Shards\.";
        string vrEssence = @"^You receive the Essence of\s+(.+?)\.";


        static Dictionary<string, string> ktdict = new Dictionary<string, string>
        {
            { "Pyre Minions", "Graveyard Skeletons" },
            { "Pyre Skeletons", "Graveyard Skeletons" },
            { "Pyre Champions", "Graveyard Skeletons" },
            { "Wights", "Graveyard Wights" },
            { "Wight Captains", "Graveyard Wights" },
            { "Wight Blade Sorcerers", "Graveyard Wights" },
            { "Despair Wisps", "Graveyard Wisps" },
            { "Hatred Wisps", "Graveyard Wisps" },
            { "Sorrow Wisps", "Graveyard Wisps" },
            { "Corrupted Dreads", "Graveyard Spirits" },
            { "Spectral Dreads", "Graveyard Spirits" },

            { "Rift of Blind Rages", "Rynthid Rifts" },
            { "Rift of Consuming Torments", "Rynthid Rifts" },
            { "Rift of Torments", "Rynthid Rifts" },
            { "Rynthid Berserkers", "Rynthid Rare Boss" },
            { "Rynthid Ravagers", "Rynthid Rare Boss" },
            { "Aspect of Rages", "Rynthid Rare Boss" },
            { "Aspect of Torments", "Rynthid Rare Boss" },
            { "Empowered Sorrow Wisps", "Rynthid Empowered Wisps" },
            { "Empowered Hatred Wisps", "Rynthid Empowered Wisps" },
            { "Empowered Despair Wisps", "Rynthid Empowered Wisps" },
            { "Rynthid Minion of Rages", "Rynthid Minions" },
            { "Raging Rynthid Sorcerers", "Rynthid Sorcerers" },

            { "Spectral Voidmages", "Spectral Mages" },
            { "Spectral Bloodmages", "Spectral Mages" },
            { "Spectral Blade Adepts", "Spectral Claws" },
            { "Spectral Blade Masters", "Spectral Claws" },
            { "Spectral Claw Adepts", "Spectral Claws" },
            { "Spectral Claw Masters", "Spectral Claws" },
            { "Bronze Golem Samurais", "Golem Samurais"},
            { "Clay Golem Samurais", "Golem Samurais"},
            { "Iron Golem Samurais", "Golem Samurais"},

            { "Frozen Wight Sorcerers", "Frozen Wights" },
            { "Frozen Wight Captains", "Frozen Wights" },
            { "Frozen Wight Archers", "Frozen Wights" },

            { "Zefir Thorn Poisoners", "Zefir Thorns"},
            { "Zefir Thorn Rangers", "Zefir Thorns"},
            { "Zefir Thorn Reavers", "Zefir Thorns"},
            { "Zefir Thorn Stalkers", "Zefir Thorns"},
            { "Brier Wasp Swarms", "Brier Wasps"},
            { "Poisonous Brier Wasps", "Brier Wasps"},
            { "Venomous Brier Wasps", "Brier Wasps"},
            { "A'nekshen Storm Callers", "A'nekshens"},
            { "A'nekshen Storm Reavers", "A'nekshens"},
            { "A'nekshen Thorn Dancers", "A'nekshens"},
            { "A'nekshen Thorn Reavers", "A'nekshens"},
            { "A'nekshen Tenders", "A'nekshens"},
            { "A'nekshen Caretaker", "A'nekshens"},

        };

        Dictionary<string, List<string>> reverseDict = ktdict
            .GroupBy(x => x.Value)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Key).ToList());

        public KillTaskUI(PluginCore plugin)
        {
            var assembly = Assembly.GetExecutingAssembly();

            string resourceName = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith("060011FA.png", StringComparison.OrdinalIgnoreCase));
            if (resourceName == null) throw new FileNotFoundException("Embedded resource 060011FA.png not found.");

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                iconGreenPlus = new ManagedTexture(stream);
            }

            resourceName = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith("arrow-up-inv.png", StringComparison.OrdinalIgnoreCase));
            if (resourceName == null) throw new FileNotFoundException("Embedded resource arrow-up-inv.png not found.");

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                pointerArrow = new ManagedTexture(stream);
            }
            Shapes = new Dictionary<int, (string Name, D3DObj D3DObj)>();


            _plugin = plugin;
            
            hud = UBService.Huds.CreateHud("Killtask Tracker");
            //hud.WindowSettings = ImGuiWindowFlags.NoNavFocus;
            hud.DontDrawDefaultWindow = true;
            hud.ShowInBar = true;
            hud.OnRender += Hud_OnRender;

            KtQuest.Init();
            QuestFlag.Init();

            CoreManager.Current.ChatBoxMessage += Current_ChatBoxMessage;
            CoreManager.Current.WorldFilter.CreateObject += WorldFilter_CreateObject;
            CoreManager.Current.WorldFilter.ReleaseObject += WorldFilter_ReleaseObject;
            
        }
        private void Hud_OnRender(object sender, EventArgs e)
        {
            try
            {
                
                windowIsOpen = hud.Visible;

                ImGui.SetNextWindowPos(new Vector2(_plugin.ktPosX.Value, _plugin.ktPosY.Value), ImGuiCond.FirstUseEver);
                ImGui.SetNextWindowSize(new Vector2(_plugin.ktSizeX.Value, _plugin.ktSizeY.Value), ImGuiCond.FirstUseEver);

                ImGui.SetNextWindowSizeConstraints(
                    new Vector2(200, 100),  // min size
                    new Vector2(450, 700)   // max size
                );

                ImGui.Begin("Kill task Tracker" + "###" + "Kill task Tracker", ref windowIsOpen, ImGuiWindowFlags.NoNavFocus);

                if (ImGui.BeginTabBar("Kill Tasks"))
                {
                    if (ImGui.BeginTabItem("Hosh"))
                    {
                        drawTab("Hoshino");
                        ImGui.EndTabItem();
                    }
                    if (ImGui.BeginTabItem("Tou-Tou"))
                    {
                        drawTab("Tou-Tou");
                        ImGui.EndTabItem();
                    }
                    if (ImGui.BeginTabItem("Rynthid"))
                    {
                        drawTab("Rynthid");
                        ImGui.EndTabItem();
                    }
                    if (ImGui.BeginTabItem("Viridian"))
                    {
                        drawTab("Viridian Rise");
                        ImGui.EndTabItem();
                    }
                    if (ImGui.BeginTabItem("Graveyard"))
                    {
                        drawTab("Graveyard");
                        ImGui.EndTabItem();
                    }
                    if (ImGui.BeginTabItem("Frozen Valley"))
                    {
                        drawTab("Frozen Valley");
                        ImGui.EndTabItem();
                    }
                    ImGui.EndTabBar();
                }
                if (ImGui.Button("Refresh"))
                {
                    QuestFlag.Refresh();
                }
                if (QuestFlag.QuestsChanged)
                {
                    deleteAllShapes();
                    addExistingShapes();
                    QuestFlag.QuestsChanged = false;
                }
                ImGui.SetWindowSize(new Vector2(ImGui.GetWindowSize().X, ImGui.GetCursorPosY() + 5));

                var wpos = ImGui.GetWindowPos();
                var wsize = ImGui.GetWindowSize();
                _plugin.ktPosX.SetValue(wpos.X);
                _plugin.ktPosY.SetValue(wpos.Y);
                _plugin.ktSizeX.SetValue(wsize.X);
                _plugin.ktSizeY.SetValue(wsize.Y);

                ImGui.End();
                hud.Visible = windowIsOpen;
            }
            catch (Exception ex)
            {
                PluginCore.Log(ex);
            }
        }

        private void drawTab(string area)
        {
            foreach (var kt in KtQuest.KtQuests.Where(x => x.Area == area))
            {
                var pos = ImGui.GetCursorScreenPos();
                bool point = _plugin.ktPoint.Value;
                WorldObject closestMobWo = null;
                var ktStatus = kt.Status();
                switch (ktStatus)
                {
                    case KtQuest.KtStatus.SingleReady:
                        ImGui.Image(iconGreenCircle.TexturePtr, siz16);
                        point = false;
                        break;
                    case KtQuest.KtStatus.TurnIn:
                        ImGui.Image(iconGreenArrow.TexturePtr, siz16);
                        point = false;
                        break;
                    case KtQuest.KtStatus.Ready:
                        if (point) closestMobWo = selectClosestMob(kt.Name);
                        if (closestMobWo == null)
                        {
                            ImGui.Image(iconGreenCircle.TexturePtr, siz16);
                        }
                        else
                        {
                            ImGui.Dummy(siz16);
                        }
                        break;
                    case KtQuest.KtStatus.NotReady:
                        ImGui.Image(iconRedX.TexturePtr, siz16);
                        point = false;
                        break;
                    case KtQuest.KtStatus.Counting:
                        if (point) closestMobWo = selectClosestMob(kt.Name);
                        if (closestMobWo == null)
                        {
                            ImGui.Image(iconGreenPlus.TexturePtr, siz16);
                        }
                        else
                        {
                            ImGui.Dummy(siz16);
                        }
                        break;
                    default:
                        break;
                }

                ImGui.SameLine();
                if (point)
                {
                    if (closestMobWo != null)
                    {

                        ImGui.SetCursorPosX(0);

                        var size = new Vector2(16, 16);
                        //ImGui.Dummy(size);

                        var me = CoreManager.Current.WorldFilter[CoreManager.Current.CharacterFilter.Id];
                        var angleToTarget = Geometry.CalculateHeadingWithLandblock(
                            (float)closestMobWo.Coordinates().EastWest,
                            (float)closestMobWo.Coordinates().NorthSouth,
                            (float)me.Coordinates().EastWest,
                            (float)me.Coordinates().NorthSouth);

                        angleToTarget = DegToRad((float)angleToTarget);

                        var playerHeading = CoreManager.Current.Actions.HeadingRadians;

                        double relative = angleToTarget - playerHeading;
                        relative = (float)Math.Atan2(Math.Sin(relative), Math.Cos(relative));
                        relative += Math.PI / 1f;

                        ImageRotated(pointerArrow.TexturePtr, pos, size, relative);
                        ImGui.SameLine();
                    }
                }


                if (ImGui.Selectable(kt.Name))
                {
                    if (ktStatus == KtQuest.KtStatus.Counting || ktStatus == KtQuest.KtStatus.Ready)
                    {
                        var w = selectClosestMob(kt.Name);
                        if (w != null)
                        {
                            CoreManager.Current.Actions.SelectItem(w.Id);
                        }
                    }
                    else
                    {
                        SelectNPC(kt.NPC);
                    }

                }
                ImGui.SameLine();
                if (kt.MaxSolves() != 0)
                {
                    ImGui.Text(kt.Solves().ToString() + "/" + kt.MaxSolves().ToString());
                    ImGui.SameLine();
                }

                ImGui.Text(kt.NextAvailable().ToString());


            }
        }
        private void WorldFilter_ReleaseObject(object sender, ReleaseObjectEventArgs e)
        {
            if (Shapes.ContainsKey(e.Released.Id))
            {
                Shapes[e.Released.Id].D3DObj.Dispose();
                Shapes.Remove(e.Released.Id);
            }
        }

        private void WorldFilter_CreateObject(object sender, CreateObjectEventArgs e)
        {
            if (!_plugin.ktMark.Value) return;
            if (e.New.ObjectClass != Decal.Adapter.Wrappers.ObjectClass.Monster) return;

            var result = KtQuest.KtQuests.Where(x => x.Active())
                        .SelectMany(x =>
                            reverseDict.ContainsKey(x.Name)
                                ? reverseDict[x.Name].Append(x.Name)
                                : new[] { x.Name }
                        )
                        .Distinct()
                        .ToList();

            if (!result.Contains(e.New.Name+"s")) return;

            addShapeById(e.New.Id, e.New.Name);
        }

        private void addShapeById(int id,string name)
        {
            if (!Shapes.ContainsKey(id))
            {
                D3DObj tmp = CoreManager.Current.D3DService.MarkObjectWithShape(id, D3DShape.VerticalArrow, Color.LightGreen.ToArgb());
                tmp.Visible = true;
                Shapes.Add(id, (name, tmp));
            }
        }
        private void deleteShapesByMob(string mob)
        {
            var mobShapeKeys = Shapes
                .Where(x => x.Value.Name == mob)
                .Select(x => x.Key)
                .ToList();
            foreach (var mobShapeKey in mobShapeKeys) {
                Shapes[mobShapeKey].D3DObj.Dispose();
                Shapes.Remove(mobShapeKey);
            }
        }

        public void deleteAllShapes()
        {
            var mobShapeKeys = Shapes
                .Select(x => x.Key)
                .ToList();
            foreach (var mobShapeKey in mobShapeKeys)
            {
                Shapes[mobShapeKey].D3DObj.Dispose();
                Shapes.Remove(mobShapeKey);
            }
        }

        public void addExistingShapes()
        {
            foreach (var item in CoreManager.Current.WorldFilter.GetByObjectClass(Decal.Adapter.Wrappers.ObjectClass.Monster))
            {
                var moblist = KtQuest.KtQuests.Where(x => x.Active())
                        .SelectMany(x =>
                            reverseDict.ContainsKey(x.Name)
                                ? reverseDict[x.Name].Append(x.Name)
                                : new[] { x.Name }
                        )
                        .Distinct()
                        .ToList();

                if (moblist.Contains(item.Name + "s"))
                {
                    addShapeById(item.Id, item.Name);
                }
            }
        }

        private WorldObject selectClosestMob(string name)
        {
            List<string> list = new List<string>();
            list.Add(name);
            if (reverseDict.ContainsKey(name))
            {
                list.AddRange(reverseDict[name]);
            }           
                
            WorldObject wo = GetClosestObject(list, Decal.Adapter.Wrappers.ObjectClass.Monster);
            if (wo == null) return null;
            return wo;
        }

        public static WorldObject GetClosestObject(List<string> objectName, Decal.Adapter.Wrappers.ObjectClass objectClass)
        {
            WorldObject closest = null;

            foreach (WorldObject obj in CoreManager.Current.WorldFilter.GetByObjectClass(objectClass))
            {
                if (!objectName.Contains(obj.Name+"s"))  continue;

                if (closest == null || GetDistanceFromPlayer(obj) < GetDistanceFromPlayer(closest))
                    closest = obj;
            }

            return closest;
        }
        public static double GetDistanceFromPlayer(WorldObject destObj)
        {
            if (CoreManager.Current.CharacterFilter.Id == 0)
                throw new ArgumentOutOfRangeException("destObj", "CharacterFilter.Id of 0");

            if (destObj.Id == 0)
                throw new ArgumentOutOfRangeException("destObj", "Object passed with an Id of 0");

            return CoreManager.Current.WorldFilter.Distance(CoreManager.Current.CharacterFilter.Id, destObj.Id) * 240;
        }

        public static float DegToRad(float deg)
        {
            return deg * ((float)Math.PI / 180f);
        }

        public void SelectNPC(int type)
        {
            int id = GetNPC(type);
            if(id != 0)
            {
                CoreManager.Current.Actions.SelectItem(id);
            }
        }

        public int GetNPC(int type) {
            foreach (WorldObject obj in CoreManager.Current.WorldFilter.GetByObjectClass(Decal.Adapter.Wrappers.ObjectClass.Npc))
            {
                if (obj.Type == type) return obj.Id;
            }
            return 0;
        }

        public static void ImageRotated(
        IntPtr texture,
        Vector2 pos,
        Vector2 size,
        double angleRad,
        Vector2? uv0 = null,
        Vector2? uv1 = null,
        uint col = 0xFFFFFFFF)
        {
            if (texture == IntPtr.Zero)
                return;

            if (double.IsNaN(angleRad) || double.IsInfinity(angleRad))
                return;
            var drawList = ImGui.GetWindowDrawList();
            
            var _uv0 = uv0 ?? new Vector2(0f, 0f);
            var _uv1 = uv1 ?? new Vector2(1f, 1f);

            var center = pos + size * 0.5f;

            float cos = (float)Math.Cos(angleRad);
            float sin = (float)Math.Sin(angleRad);

            var half = size * 0.5f;

            var topLeft = new Vector2(-half.X, -half.Y);
            var topRight = new Vector2(half.X, -half.Y);
            var bottomRight = new Vector2(half.X, half.Y);
            var bottomLeft = new Vector2(-half.X, half.Y);

            Vector2 Rotate(Vector2 p)
            {
                return new Vector2(
                    center.X + p.X * cos - p.Y * sin,
                    center.Y + p.X * sin + p.Y * cos
                );
            }

            var r0 = Rotate(topLeft);
            var r1 = Rotate(topRight);
            var r2 = Rotate(bottomRight);
            var r3 = Rotate(bottomLeft);

            if (float.IsNaN(r0.X) || float.IsNaN(r0.Y)) return;

            drawList.AddImageQuad(
                texture,
                r0, r1, r2, r3,
                new Vector2(_uv0.X, _uv0.Y),
                new Vector2(_uv1.X, _uv0.Y),
                new Vector2(_uv1.X, _uv1.Y),
                new Vector2(_uv0.X, _uv1.Y),
                col
            );
        }

        private void Current_ChatBoxMessage(object sender, ChatTextInterceptEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(e.Text)) return;

                if (QuestFlag.MyQuestRegex.IsMatch(e.Text))
                {
                    QuestFlag.Add(e.Text);
                }

                // You have killed 50 Drudge Raveners! Your task is complete!
                if (e.Text.StartsWith("You have killed ") && e.Text.Trim().EndsWith("Your task is complete!"))
                {
                    Match match = Regex.Match(e.Text, donePattern);

                    if (match.Success)
                    {
                        int count = int.Parse(match.Groups[1].Value);
                        string name = match.Groups[2].Value;
                        name = ktdict.TryGetValue(name, out var value) ? value : name;

                        int index2 = KtQuest.KtQuests.FindIndex(x => x.Name == name);

                        if (index2 >= 0)
                        {
                            KtQuest.KtQuests[index2].UpdateSolves(count);
                        }
                        List<string> mobs = new List<string>();
                        mobs.Add(name);
                        if (reverseDict.ContainsKey(name))
                        {
                            mobs.AddRange(reverseDict[name]);
                        }
                        

                        foreach(var mob in mobs)
                        {
                            string minuss = mob.Substring(0, mob.Length - 1);
                            deleteShapesByMob(minuss);
                        }
                    }
                }

                //You have killed 1 Panumbris Shadows! You must kill 25 to complete your task.
                if (e.Text.StartsWith("You have killed ") && e.Text.Trim().EndsWith("to complete your task."))
                {
                    Match match = Regex.Match(e.Text, pattern);

                    if (match.Success)
                    {
                        int firstNumber = int.Parse(match.Groups[1].Value);
                        string name = match.Groups[2].Value;
                        name = ktdict.TryGetValue(name, out var value) ? value : name;
                        int lastNumber = int.Parse(match.Groups[3].Value);

                        int index2 = KtQuest.KtQuests.FindIndex(x => x.Name == name);

                        if (index2 >= 0)
                        {
                            KtQuest.KtQuests[index2].UpdateSolves(firstNumber);
                        }

                    }
                }

                //You have killed 1 Panumbris Shadows! You must kill 25 to complete your task.
                // You have killed 50 Drudge Raveners! Your task is complete!
                //You've killed 5 out of 25 Shadows.
                //mid
                if (e.Text.StartsWith("You've killed "))
                {
                    Match match = Regex.Match(e.Text, midPattern);

                    if (match.Success)
                    {
                        int firstNumber = int.Parse(match.Groups[1].Value);
                        string name = match.Groups[3].Value;
                        name = ktdict.TryGetValue(name, out var value) ? value : name;
                        int lastNumber = int.Parse(match.Groups[2].Value);

                        int index2 = KtQuest.KtQuests.FindIndex(x => x.Name == name);

                        if (index2 >= 0)
                        {
                            KtQuest.KtQuests[index2].UpdateSolves(firstNumber);
                        }

                    }
                }
                
                if (e.Text.StartsWith("Viridian Portal gives you ") && e.Text.Trim().EndsWith(" Infused Amber Shards."))
                {
                    Match match = Regex.Match(e.Text, vrPortal);

                    if (match.Success)
                    {
                        int count = int.Parse(match.Groups[1].Value);
                                                
                        int index2 = KtQuest.KtQuests.FindIndex(x => x.Name.StartsWith("Portal") && x.Current == int.Parse(match.Groups[1].Value));

                        if (index2 >= 0)
                        {
                            KtQuest.KtQuests[index2].UpdateCompletedOn();
                        }

                    }
                }

                if (e.Text.StartsWith("You receive the Essence of "))
                {
                    Match match = Regex.Match(e.Text, vrEssence);

                    if (match.Success)
                    {
                        string name = match.Groups[1].Value;
                        
                        int index2 = KtQuest.KtQuests.FindIndex(x => x.Name == name);

                        if (index2 >= 0)
                        {
                            KtQuest.KtQuests[index2].UpdateCompletedOn();
                        }

                    }
                }


            }
            catch (Exception ex)
            {
                PluginCore.Log(ex);
            }
        }
        public void setVisibility(bool visibility)
        {
            hud.Visible = visibility;
            hud.ShowInBar = visibility;
        }
        public void Dispose()
        {
            deleteAllShapes();
            hud.Dispose();

            CoreManager.Current.ChatBoxMessage -= Current_ChatBoxMessage;
            CoreManager.Current.WorldFilter.CreateObject -= WorldFilter_CreateObject;
            CoreManager.Current.WorldFilter.ReleaseObject -= WorldFilter_ReleaseObject;
        }
    }
}