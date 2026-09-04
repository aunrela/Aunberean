using ACE.Entity.Models;
using Decal.Adapter;
using Decal.Adapter.Wrappers;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UtilityBelt.Common.Enums;
using UtilityBelt.Scripting.Actions;
using UtilityBelt.Scripting.Interop;
using Vital = UtilityBelt.Scripting.Interop.Vital;
using WorldObject = UtilityBelt.Scripting.Interop.WorldObject;

namespace Aunberean
{
    public class OneTouchHeal
    {
        public static List<HealKit> HealKits = new List<HealKit>();
        Game game;
        public OneTouchHeal(List<HealKit> hk)
        {
            game = PluginCore.game;
            HealKits.AddRange(hk);
            //HealKits = hk;
            populateIds();
        }
        public void Dispose()
        {

        }
        public void Add(WorldObject item)
        {
            HealKit newHK = new HealKit();
            newHK.id = item.Id;
            newHK.name = item.Name;
            newHK.icon = item.Value(DataId.Icon);
            newHK.type = item.ClassId;
            if (item.ObjectClass == UtilityBelt.Scripting.Enums.ObjectClass.Food)
            {
                newHK.food = true;
                newHK.bonusSkill = item.Value(IntId.BoostValue);
            }
            else
            {
                newHK.food = false;
                newHK.bonusSkill = item.Value(IntId.BoostValue);
                newHK.setChance = 97.0f;
                newHK.bonusHeal = item.Value(FloatId.HealkitMod);
            }
            HealKits.Add(newHK);
        }
        public void Heal()
        {
            foreach (HealKit kit in HealKits)
            {
                Vital health = null;

                if (game.Character.Weenie.Vitals.TryGetValue(VitalId.Health, out health))
                {
                    if (health.Max - health.Current == 0)
                    {
                        //CoreManager.Current.Actions.AddChatText("HP is full didnt heal", 1);
                        break;
                    }
                }


                var item = kit.getItem();
                if (item == null) continue;

                if (kit.food == true)
                {
                    if (item.CooldownTimeoutMilliseconds > 0) continue;
                    CoreManager.Current.Actions.UseItem((int)kit.id, 0);
                    break;
                }

                if (kit.healChance() >= kit.setChance)
                {
                    CoreManager.Current.Actions.ApplyItem((int)kit.id, CoreManager.Current.CharacterFilter.Id);
                    break;
                }
            }

        }
        private void populateIds()
        {
            foreach (HealKit kit in HealKits)
            {
                kit.getItem();
            }
        }
        public int GetHealingSkill()
        {
            Skill value = null;
            if (PluginCore.game.Character.Weenie.Skills.TryGetValue(SkillId.Healing, out value))
            {
                return value.Current;
            }

            return 0;
        }

        public int GetHealBoostRating()
        {
            return PluginCore.game.Character.Weenie.Value(IntId.HealingBoostRating);
        }
        public int MissingHP()
        {
            int healthPointsFromMax = 0;

            Vital health = null;
            if (PluginCore.game.Character.Weenie.Vitals.TryGetValue(VitalId.Health, out health))
            {
                healthPointsFromMax = health.Max - health.Current;
            }
            return healthPointsFromMax;
        }
    }
    public class HealKit
    {
        [Newtonsoft.Json.JsonIgnore]
        public uint id;
        [Newtonsoft.Json.JsonIgnore]
        private bool idRequested;
        public string name;
        public int _bonusSkill;
        [Newtonsoft.Json.JsonIgnore]
        public int bonusSkill
        {
            get
            {
                if (this._bonusSkill == 0)
                {
                    getItem();
                }
                return _bonusSkill;
            }
            set { _bonusSkill = value; }
        }
        public float _bonusHeal;
        [Newtonsoft.Json.JsonIgnore]
        public float bonusHeal
        {
            get
            {
                if (this._bonusHeal == 0)
                {
                    getItem();
                }
                return _bonusHeal;
            }
            set { _bonusHeal = value; }
        }
        public uint type;
        public uint icon;
        public bool food;
        public float setChance;

        public HealKit()
        {

        }
        public HealKit(string name, int bonusSkill, float bonusheal, uint type, uint icon, bool food, float setChance)
        {
            this.name = name;
            this.bonusSkill = bonusSkill;
            this.bonusHeal = bonusheal;
            this.type = type;
            this.icon = icon;
            this.food = food;
            this.setChance = setChance;
        }
        public WorldObject getItem()
        {
            var item = PluginCore.game.Character.Inventory.Where(x => x.Id == id).FirstOrDefault();
            if (item != null)
            {
                if (!item.HasAppraisalData && !idRequested)
                {
                    idRequested = true;
                    item.Appraise(null, OnAppraiseComplete);
                    return item;
                }

                id = item.Id;
                bonusSkill = item.Value(IntId.BoostValue);
                bonusHeal = item.Value(FloatId.HealkitMod);
                return item;
            }

            var replacementItem = PluginCore.game.Character.Inventory.Where(x => x.ClassId == type).OrderBy(y => y.Value(IntId.Structure)).FirstOrDefault();
            if (replacementItem != null)
            {
                if (!replacementItem.HasAppraisalData && !idRequested)
                {
                    idRequested = true;
                    replacementItem.Appraise(null, OnAppraiseComplete);
                    return replacementItem;
                }
                id = replacementItem.Id;
                bonusSkill = replacementItem.Value(IntId.BoostValue);
                bonusHeal = replacementItem.Value(FloatId.HealkitMod);
                return replacementItem;
            }

            return null;
        }

        private void OnAppraiseComplete(ObjectAppraiseAction action)
        {
            idRequested = false;
            
            var item = PluginCore.game.World.Get(action.ObjectId);
            if (item != null && item.HasAppraisalData)
            {
                bonusSkill = item.Value(IntId.BoostValue);
                bonusHeal = item.Value(FloatId.HealkitMod);
            }
        }

        public int GetHealingSkill()
        {
            Skill value = null;
            if (PluginCore.game.Character.Weenie.Skills.TryGetValue(SkillId.Healing, out value))
            {
                return value.Current;
            }

            return 0;
        }
        public int GetHealBoostRating()
        {
            return PluginCore.game.Character.Weenie.Value(IntId.HealingBoostRating);
        }
        public double healChance()
        {
            int healingSkill = (int)((GetHealingSkill() + bonusSkill) * 1.1);

            int difficulty;

            int healthPointsFromMax = 0;

            Vital health = null;
            if (PluginCore.game.Character.Weenie.Vitals.TryGetValue(VitalId.Health, out health))
            {
                healthPointsFromMax = health.Max - health.Current;
            }
            if (healthPointsFromMax == 0)
            {
                return 100;
            }
            bool peace = PluginCore.game.Character.CombatMode == CombatMode.NonCombat;
            if (peace)
                difficulty = healthPointsFromMax * 2;
            else
                difficulty = (int)(healthPointsFromMax * 2 * 1.1f);

            return GetSkillChance(healingSkill, difficulty);
        }

        public static double GetSkillChance(int skill, int difficulty, float factor = 0.03f)
        {
            var chance = 1.0 - 1.0 / (1.0 + Math.Exp(factor * (skill - difficulty)));
            chance *= 100;
            return Math.Min(100.0, Math.Max(0.0, chance));
        }

        public double GetDifficulty(int bonus, double chance, float factor = 0.03f)
        {
            Skill trained;
            PluginCore.game.Character.Weenie.Skills.TryGetValue(SkillId.Healing, out trained);
            if (trained == null) { return 0.0; }
            float trainedMod = trained.Training == SkillTrainingType.Specialized ? 1.5f : 1.1f;
            int healingSkill = (int)((GetHealingSkill() + bonus) * trainedMod);

            double p = chance / 100.0;

            if (p < 0.000001) p = 0.000001;
            if (p > 0.999999) p = 0.999999;

            var difficulty = healingSkill - (Math.Log(p / (1.0 - p)) / factor);

            bool peace = PluginCore.game.Character.CombatMode == CombatMode.NonCombat;
            if (peace)
                difficulty /= 2;
            else
            {
                difficulty /= 2.0f;
                difficulty /= 1.1f;
            }

            return difficulty;
        }
        public double getMissingHPatChance()
        {
            return GetDifficulty(bonusSkill, setChance);
        }
        public (int, int) getAmountHealed()
        {
            int healBoostRating = GetHealBoostRating();
            float healBoostRatingPercent = (float)(healBoostRating + 100) / 100;
            int min = (int)(GetHealingSkill() * bonusHeal * .2f * healBoostRatingPercent);
            int max = (int)(GetHealingSkill() * bonusHeal * .5f * healBoostRatingPercent);

            return (min, max);
        }

    }
}
