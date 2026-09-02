using Aunberean;
using Decal.Adapter;
using Decal.Interop.Core;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public static class KtQuestEditor
{
    private static int selectedQuest = -1;
    private static string newMobName = "";

    public static void Draw()
    {
        ImGui.BeginChild(
            "QuestList",
            new Vector2(250, 500));

        ImGui.Text($"Quests: {KtQuest.KtQuests.Count}");
        ImGui.Separator();
        if (ImGui.Button("Save"))
        {
            PluginCore._plugin.ktui.saveList();
        }

        ImGui.SameLine();

        if (ImGui.Button("Reload Default KTs"))
        {
            ImGui.OpenPopup("Reload Defaults?");
            
        }

        if (ImGui.BeginPopupModal(
            "Reload Defaults?"))
        {
            ImGui.Text("Are you sure you want to reload default quests?");
            ImGui.Text("This action cannot be undone.");

            ImGui.Spacing();

            if (ImGui.Button("Reload"))
            {
                PluginCore._plugin.ktui.clearList();

                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();

            if (ImGui.Button("Cancel"))
            {
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

        if (ImGui.Button("+ New Quest", new Vector2(-1, 0)))
        {
            KtQuest.KtQuests.Add(new KtQuest
            {
                Name = "New Quest",
                Area = "Unknown Area"
            });

            selectedQuest = KtQuest.KtQuests.Count - 1;
        }

        ImGui.Separator();

        var groupedQuests = KtQuest.KtQuests
            .Select((quest, index) => new
            {
                Quest = quest,
                Index = index
            })
            .GroupBy(x => string.IsNullOrWhiteSpace(x.Quest.Area)
                ? "Unknown Area"
                : x.Quest.Area)
            .OrderBy(x => x.Key);

        foreach (var areaGroup in groupedQuests)
        {
            if (ImGui.TreeNodeEx(
                areaGroup.Key,
                ImGuiTreeNodeFlags.None))
            {
                foreach (var item in areaGroup)
                {
                    var quest = item.Quest;
                    int index = item.Index;

                    string label = string.IsNullOrWhiteSpace(quest.Name)
                        ? $"Quest {index + 1}"
                        : quest.Name;

                    if (ImGui.Selectable(
                        label,
                        selectedQuest == index))
                    {
                        selectedQuest = index;
                    }
                }

                ImGui.TreePop();
            }
        }

        ImGui.EndChild();

        ImGui.SameLine();

        ImGui.BeginChild(
            "QuestEditor",
            new Vector2(300, 0));

        if (selectedQuest < 0 ||
            selectedQuest >= KtQuest.KtQuests.Count)
        {
            ImGui.Text("Select a quest to edit.");
            ImGui.EndChild();
            return;
        }

        var questToEdit = KtQuest.KtQuests[selectedQuest];

        DrawQuestEditor(questToEdit);
        
        ImGui.EndChild();
    }

    private static void DrawQuestEditor(KtQuest quest)
    {
        ImGui.Text("Quest Editor");
        ImGui.Separator();

        ImGui.Checkbox("Enabled", ref quest.Enabled);
        
        DrawStringInput("Name - for UI display", ref quest.Name);

        DrawStringInput("Area - for UI display", ref quest.Area);

        ImGui.Spacing();

        DrawStringInputLower(
                "Quest Complete Flag",
                ref quest.QuestFlagComplete);

        DrawStringInputLower(
            "Quest Counts Flag",
            ref quest.QuestFlagCounts);

        ImGui.Spacing();

        string value = quest.NPC.ToString();
        ImGui.Text("NPC ID");
        if (ImGui.InputText("###NPC ID", ref value, 32, ImGuiInputTextFlags.CharsDecimal))
        {
            if (int.TryParse(value, out int result))
                quest.NPC = result;
        }

        if(ImGui.Button("Add Selected NPC"))
        {
            if(CoreManager.Current.Actions.CurrentSelection != 0)
            {
                var type = CoreManager.Current.WorldFilter[CoreManager.Current.Actions.CurrentSelection].Type;
                quest.NPC = type;
            }
        }

        ImGui.Spacing();

        DrawMobNamesEditor(quest);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.PushStyleColor(
            ImGuiCol.Button,
            new Vector4(0.7f, 0.1f, 0.1f, 1));

        ImGui.PushStyleColor(
            ImGuiCol.ButtonHovered,
            new Vector4(0.9f, 0.15f, 0.15f, 1));

        ImGui.PushStyleColor(
            ImGuiCol.ButtonActive,
            new Vector4(0.6f, 0.05f, 0.05f, 1));

        if (ImGui.Button("Delete Quest"))
        {
            ImGui.OpenPopup("Delete Quest?");
        }

        ImGui.PopStyleColor(3);

        if (ImGui.BeginPopupModal(
            "Delete Quest?"))
        {
            ImGui.Text("Are you sure you want to delete this quest?");
            ImGui.Text("This action cannot be undone.");

            ImGui.Spacing();

            if (ImGui.Button("Delete"))
            {
                KtQuest.KtQuests.RemoveAt(selectedQuest);

                if (selectedQuest >= KtQuest.KtQuests.Count)
                {
                    selectedQuest = KtQuest.KtQuests.Count - 1;
                }

                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();

            if (ImGui.Button("Cancel"))
            {
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }
    }

    private static void DrawMobNamesEditor(KtQuest quest)
    {
        ImGui.Text("Mob names:");
        ImGui.Spacing();

        for (int i = 0; i < quest.MobNames.Count; i++)
        {
            ImGui.PushID($"MobName_{i}");

            ImGui.SetNextItemWidth(-45);

            string mobName = quest.MobNames[i];

            if (ImGui.InputText(
                "##MobName",
                ref mobName,
                256))
            {
                quest.MobNames[i] = mobName;
            }

            ImGui.SameLine();

            if (ImGui.Button("X"))
            {
                quest.MobNames.RemoveAt(i);

                ImGui.PopID();
                break;
            }

            ImGui.PopID();

            ImGui.Spacing();
        }

        ImGui.Separator();
        ImGui.Spacing();

        ImGui.Text("Add Mob Name");

        ImGui.SetNextItemWidth(-80);

        ImGui.InputText(
            "##NewMob",
            ref newMobName,
            256);

        ImGui.SameLine();

        if (ImGui.Button("Add"))
        {
            if (!string.IsNullOrWhiteSpace(newMobName))
            {
                quest.MobNames.Add(newMobName.Trim());
                newMobName = "";
            }
        }

        if (ImGui.Button("Add Selected Mob"))
        {
            if (CoreManager.Current.Actions.CurrentSelection != 0)
            {
                var name = CoreManager.Current.WorldFilter[CoreManager.Current.Actions.CurrentSelection].Name;
                quest.MobNames.Add(name);
            }
        }
    }

    private static void DrawStringInput(
        string label,
        ref string value)
    {
        ImGui.Text(label);

        ImGui.SetNextItemWidth(-1);

        ImGui.InputText(
            $"##{label}",
            ref value,
            1024);

        ImGui.Spacing();
    }

    private static void DrawStringInputLower(
    string label,
    ref string value)
    {
        ImGui.Text(label);

        ImGui.SetNextItemWidth(-1);

        string input = value;

        if (ImGui.InputText(
            $"##{label}",
            ref input,
            1024))
        {
            value = input.ToLowerInvariant().Trim();
        }

        ImGui.Spacing();
    }

    private static void DrawIntInput(
        string label,
        ref int value)
    {
        ImGui.Text(label);

        ImGui.SetNextItemWidth(-1);

        ImGui.InputInt(
            $"##{label}",
            ref value);

        ImGui.Spacing();
    }
}