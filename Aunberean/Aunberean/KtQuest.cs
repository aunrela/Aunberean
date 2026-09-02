using Decal.Adapter;
using Decal.Adapter.Wrappers;
using Decal.Filters;
using ImGuiNET;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Aunberean
{
    public class KtQuest
    {
        public static List<KtQuest> KtQuests = new List<KtQuest>();

        // Properties
        public string Area = "";
        public string Name = "";
        public string QuestFlagComplete = "";
        public string QuestFlagCounts = "";
        public string QuestGreenText = "";
        public int Current = 0;
        public int NPC = 0;
        public List<string> MobNames = new List<string>();
        public bool Enabled = true;

        public static void Init(List<KtQuest> ktQuests)
        {
            KtQuests.Clear();
            if (ktQuests.Count != 0)
            {
                KtQuests = ktQuests;
            }
            else {
                LoadKtQuests();
            }
                
        }

        public static void LoadKtQuests()
        {
            var quests = new List<KtQuest>();

            var assembly = Assembly.GetExecutingAssembly();

            string resourceName = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith("defaultKTQuests.json", StringComparison.OrdinalIgnoreCase));
            if(resourceName == null) throw new FileNotFoundException("Embedded resource defaultKTQuests.json not found.");
            
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                string json = reader.ReadToEnd();
                quests = JsonConvert.DeserializeObject<List<KtQuest>>(json) ?? new List<KtQuest>(); ;
            }

            KtQuests.AddRange(quests);
        }

        public new string ToString()
        {
            return $"{Name}: {QuestFlagComplete} QuestFlagComplete:{QuestFlagComplete}";
        }

        public string think()
        {
            QuestFlag.QuestFlags.TryGetValue(QuestFlagComplete, out QuestFlag questFlag);
            if (questFlag == null) { return ""; }

            if (questFlag.Solves >= questFlag.MaxSolves)
            {
                return ($"Quest: {questFlag.Key} is completed");
            }
            else
            {
                return($"Quest: {questFlag.Key} is at {questFlag.Solves} of {questFlag.MaxSolves}");
            }
        }

        public bool Active()
        {

            if(!Ready()) return false;
            if(MaxSolves() == 0) return false;
            if(Solves() == MaxSolves()) return false;

            return true;
        }

        public bool IsComplete()
        {
            QuestFlag.QuestFlags.TryGetValue(QuestFlagComplete, out QuestFlag questFlag);
            if (questFlag == null) { return false; }

            // Check if the BitMask is set in solves
            return false;
            //return (questFlag.Solves & BitMask) == BitMask;
        }

        public DateTime? CompletedOn()
        {
            QuestFlag.QuestFlags.TryGetValue(QuestFlagComplete, out QuestFlag questFlag);
            if (questFlag == null) { return null; }

            return questFlag.CompletedOn;
        }
        public TimeSpan? NextAvailableTime()
        {
            QuestFlag.QuestFlags.TryGetValue(QuestFlagComplete, out QuestFlag questFlag);
            if (questFlag == null) { return null; }

            return questFlag.NextAvailableTime();
        }

        public string NextAvailable()
        {
            QuestFlag.QuestFlags.TryGetValue(QuestFlagComplete, out QuestFlag questFlag);
            if (questFlag == null) { return ""; }
            var ret = questFlag.NextAvailable();
            if (ret == "ready") return "";
            return ret;
        }

        public bool Ready()
        {
            QuestFlag.QuestFlags.TryGetValue(QuestFlagComplete, out QuestFlag questFlag);
            if (questFlag == null) { return true; }

            return questFlag.Ready();
        }

        public bool IsSingleIsReady()
        {
            if (QuestFlagCounts != "") return false;

            QuestFlag.QuestFlags.TryGetValue(QuestFlagComplete, out QuestFlag questFlag);
            if (questFlag == null) { 
                return true; }

            return questFlag.Ready();
        }

        public int Solves()
        {
            QuestFlag.QuestFlags.TryGetValue(QuestFlagCounts, out QuestFlag questFlag);
            if (questFlag == null) { return 0; }

            return questFlag.Solves;
        }

        public int MaxSolves()
        {
            QuestFlag.QuestFlags.TryGetValue(QuestFlagCounts, out QuestFlag questFlag);
            if (questFlag == null) { return 0; }

            return questFlag.MaxSolves;
        }

        public void UpdateSolves(int count)
        {
            QuestFlag.QuestFlags.TryGetValue(QuestFlagCounts, out QuestFlag questFlag);
            if (questFlag == null) { return; }
            questFlag.Solves = count;
            //return questFlag.MaxSolves;
        }

        public void UpdateCompletedOn()
        {
            //CoreManager.Current.Actions.AddChatText("trying updatye", 1);
            QuestFlag.QuestFlags.TryGetValue(QuestFlagComplete, out QuestFlag questFlag);
            if (questFlag == null) {
                //CoreManager.Current.Actions.AddChatText("not found", 1); 
                return; }
            questFlag.CompletedOn = DateTime.UtcNow;
            //CoreManager.Current.Actions.AddChatText("completed" + questFlag.CompletedOn, 1);
            //return questFlag.CompletedOn;
        }
        public KtStatus Status()
        {
            if (IsSingleIsReady())
            {
                return KtStatus.SingleReady;
            }
            else if (Ready() && Solves() == MaxSolves() && MaxSolves() != 0)
            {
                return KtStatus.TurnIn;
            }
            else if (Ready() && Solves() == 0 && MaxSolves() != 0)
            {
                return KtStatus.Ready;
            }
            else if (!Ready() || MaxSolves() == 0)
            {
                return KtStatus.NotReady;
            }
            else if (Ready())
            {
                return KtStatus.Counting;
            }
            return KtStatus.NotReady;
        }
        public enum KtStatus
        {
            SingleReady,
            Ready,
            Counting,
            TurnIn,
            NotReady,
        }
    }
}

