using Game.Systems.Item;
using Game.Systems.Job;
using Game.Systems.Stat;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Systems.Units
{
    [CreateAssetMenu(menuName = "Unit System/Unit Template")]
    public class UnitTemplate : ScriptableObject
    {
        public int id;
        public string templatName;
        public int factionId;
        public UnitStatsData unitStatData;
        public JobSave currentJob;
        public List<JobSave> pastJobs;
        public List<ItemSave> items;
        public List<int> equippedSkills;
    }
}

