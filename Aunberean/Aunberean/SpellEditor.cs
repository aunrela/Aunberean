using ACE.DatLoader.Entity;
using ACE.DatLoader.FileTypes;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using UtilityBelt.Service;

namespace Aunberean
{
    public static class SpellEditor
    {
        private static string _spellSearch = "";

        private static SpellTable spellTable = UBService.PortalDat.ReadFromDat<ACE.DatLoader.FileTypes.SpellTable>(0x0E00000E);

        public static void DrawSpellEditor(List<int> spellIds)
        {
            ImGui.BeginChild(
            "Selected Spells",
            new Vector2(300, 500));

            ImGui.Text("Selected Spells");
            ImGui.SameLine();
            if (ImGui.Button("Save"))
            {
                PluginCore._plugin.Settings.Save();
            }

            if (spellIds.Count == 0)
            {
                ImGui.TextDisabled("No spells selected.");
            }

            for (int i = 0; i < spellIds.Count; i++)
            {
                int spellId = spellIds[i];

                string spellName;

                if (spellTable.Spells.TryGetValue((uint)spellId, out SpellBase? spell))
                    spellName = spell.Name;
                else
                    spellName = $"Unknown Spell ({spellId})";

                ImGui.PushID(i);

                if (ImGui.Button("X"))
                {
                    spellIds.RemoveAt(i);
                    i--;
                }

                ImGui.SameLine();

                ImGui.TextDisabled($"({spellId})");

                ImGui.SameLine();

                ImGui.Text(spellName);

                ImGui.PopID();
            }
            ImGui.EndChild();
            ImGui.SameLine();

            ImGui.BeginChild(
            "Search Spells",
            new Vector2(300, 500));
            ImGui.Text("Search To Add Spells");

            ImGui.SetNextItemWidth(-1);
            ImGui.InputText(
                "##SpellSearch",
                ref _spellSearch,
                256);

            if (!string.IsNullOrWhiteSpace(_spellSearch))
            {
                foreach (var pair in spellTable.Spells)
                {
                    int spellId = (int)pair.Key;
                    SpellBase spell = pair.Value;

                    if (spell.Name.IndexOf(_spellSearch, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    ImGui.PushID((int)spellId);

                    if (spellIds.Contains(spellId))
                    {
                        ImGui.TextDisabled("Added");
                    }
                    else if (ImGui.Button("+"))
                    {
                        spellIds.Add(spellId);
                    }

                    ImGui.SameLine();

                    ImGui.TextDisabled($"({spellId})");

                    ImGui.SameLine();

                    ImGui.Text(spell.Name);

                    ImGui.PopID();
                }
            }
            ImGui.EndChild();
        }

    }
}
