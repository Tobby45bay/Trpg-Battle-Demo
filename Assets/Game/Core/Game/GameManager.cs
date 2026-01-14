using Game.Systems.BattleMap;
using Game.Systems.Combat;
using Game.Systems.Effect;
using Game.Systems.Faction;
using Game.Systems.Item;
using Game.Systems.Job;
using Game.Systems.Skill;
using Game.Systems.Tags;
using Game.Systems.Trigger;
using Game.Systems.Units;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Game.Core.Game
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;
        public TagDatabase tagDb = new();
        public TileDatabase TileDb;
        public UnitTemplateDatabase UnitDb;
        public UnitSpriteDatabase UnitSpriteDb;
        public EffectDatabase EffectDb;
        public SkillDatabase SkillDb;
        public FactionDatabase FactionDb;
        public ItemDatabase ItemDb;
        public AttackDatabase AttackDb;
        public JobDatabase JobDb;
        public MovementProfileDatabase MovementProfileDb;

        public TriggerDispatcher triggerDispatcher;

        private void Awake()
        {
            if(Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            triggerDispatcher = new TriggerDispatcher();
            InitTagDatabase();
            Init();
        }

        public void InitTagDatabase()
        {
            LoadStatTags();
            LoadTriggerCallTag();
            LoadRankTag();
        }

        

     
        public void LoadStatTags()
        {
            var statTags = new TagCategory("stats");
            statTags.AddTag(0, "Health");
            statTags.AddTag(1, "Attack");
            statTags.AddTag(2, "Defence");
            statTags.AddTag(3, "Magic");
            statTags.AddTag(4, "Resistance");
            statTags.AddTag(5, "Speed");
            statTags.AddTag(6, "Luck");
            statTags.AddSubList("CoreStats");
            for(int i = 0;i <= 6; i++)
            {
                statTags.AddTagToSubList("CoreStats", i);
            }
            statTags.AddTag(7, "Hit_Rate");
            statTags.AddTag(8, "Avoid_Rate");
            statTags.AddTag(9, "Move");

            statTags.AddSubList("OtherStats");
            statTags.AddTagToSubList("OtherStats", 7);
            statTags.AddTagToSubList("OtherStats", 8);
            statTags.AddTagToSubList("OtherStats", 9); ;
            tagDb.AddCategory(statTags);
        }

        public void LoadTriggerCallTag()
        {
            var triggerCallTags = new TagCategory("triggercall");
            triggerCallTags.AddTag(0, "OnBattleStart");
            triggerCallTags.AddTag(1, "PhaseStart");
            triggerCallTags.AddTag(2, "PhaseEnd");
            triggerCallTags.AddTag(3, "UnitTurnStart");
            triggerCallTags.AddTag(4, "UnitTurnEnd");
            triggerCallTags.AddTag(5, "TurnStart");
            triggerCallTags.AddTag(6, "OnUnitEnter");
            triggerCallTags.AddTag(7, "OnUnitExit");

            tagDb.AddCategory(triggerCallTags);
        }

        public void LoadRankTag()
        {
            var rankTags = new TagCategory("ranks");
            rankTags.AddTag(0, "E");
            rankTags.AddTag(1, "D");
            rankTags.AddTag(2, "C");
            rankTags.AddTag(3, "B");
            rankTags.AddTag(4, "A");
            rankTags.AddTag(5, "S");

            tagDb.AddCategory(rankTags);
        }

        

        public enum SourceType
        {
            Unit,
            Effect,
            Skill,
            Attack,
            Tile,
            Item,
            Weapon
        }

        [Serializable]
        public enum TargetType
        {
            Self,
            Other,
            Ally,
            Enemy,
            Target
        }

        public static class GameConst
        {
            public static class Id
            {
                public const int UnitStart = 100;
                public const int ItemStart = 300;
            }

            public static class Skills
            {
                public const int MaxSkillEquipped = 5;
            }

            public static class Combat
            {
                public const float SpeedFactor = 0.5f; // Each speed point adds 0.5% to hit or avoid
                public const float LuckFactor = 0.3f;  // Each luck point adds 0.3% to hit
                public const float CritMultiplier = 1.5f;
                public const float EffectiveMultiplier = 1.3f;

            }

            public static class Stats
            {
                public const int MaxLevel = 99;
                public const int LuckCap = 45;

            }

            public static class Effects
            {
                public const int InfiniteDuration = -1;
            }

            public static class Inventory
            {
                public const int MaxItemsPerUnit = 5;
            }
        }



        public void Init()
        {
            TileDb.Initialize();
            UnitDb.Initialize();
            EffectDb.Initialize();
            SkillDb.Initialize();
            FactionDb.Initialize();
            ItemDb.Initialize();
            AttackDb.Initialize();
            JobDb.Initialize();
            MovementProfileDb.Initialize();
        }

    }
}

