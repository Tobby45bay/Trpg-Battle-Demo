using Game.Systems.BattleMap;
using Game.Systems.Tags;
using Game.Systems.Trigger;
using Game.Systems.Units;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

namespace Game.Core.Game
{
    public class GlobalSystems
    {
        public TriggerDispatcher TriggerDispatcher;

        public GlobalSystems()
        {
            TriggerDispatcher = new TriggerDispatcher();
        }
    }

    public class GameDatabases
    {
        public TagDatabase tagDb = new();
        public TileDatabase tileDb;
        public UnitTemplateDatabase unitDb;

        public void InitTagDatabase()
        {
            LoadStatTags();
            LoadTriggerCallTag();
        }

        public void LoadStatTags()
        {
            var statTags = new TagCategory("Stats");
            statTags.AddTag(0, "Health");
            statTags.AddTag(1, "Attack");
            statTags.AddTag(2, "Defence");
            statTags.AddTag(3, "Magic");
            statTags.AddTag(4, "Resistance");
            statTags.AddTag(5, "Speed");
            statTags.AddTag(6, "Luck");
            statTags.AddTag(7, "Hit_Rate");
            statTags.AddTag(8, "Avoid_Rate");
            statTags.AddTag(9, "Move");
            tagDb.AddCategory(statTags);
        }

        public void LoadTriggerCallTag()
        {
            var triggerCallTags = new TagCategory("TriggerCall");

            tagDb.AddCategory(triggerCallTags);
        }

        public void Init()
        {
            tileDb.Initialize();
            unitDb.Initialize();
        }
    }
}
