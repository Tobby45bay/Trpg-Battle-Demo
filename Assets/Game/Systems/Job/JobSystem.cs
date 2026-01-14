using Game.Core.Battle;
using Game.Core.Game;
using Game.Systems.BattleMap;
using Game.Systems.Effect;
using Game.Systems.Stat;
using Game.Systems.Trigger;
using Game.Systems.Units;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Game.Core.Game.GameManager;

namespace Game.Systems.Job
{ 
    [Serializable]
    public class JobSave
    {
        public int JobID;
        public int jobLevel;
    }

    [Serializable]
    public class JobDataEntry
    {
        public int RequiredLevel;
        public SourceType Source;
        public int SourceId;
        public bool CanTransfer;
    }

    [Serializable]
    public class JobPromotionEntry
    {
        public int TargetJobId;
        public int RequiredJobLevel;
        public int RequiredItemId;

        public StatType RequiredStatType;
        public CoreStats CoreStat;
        public OtherStats OtherStat;
        public int RequiredStatValue;

        public StatKey GetStatKey()
        {
            return RequiredStatType == StatType.Core
                ? new StatKey(CoreStat)
                : new StatKey(OtherStat);
        }
    }

    [Serializable]
    public class MovementProfileData
    {
        public int Id;
        public string Name;

        public MoveType MoveType;
        public TerrainInteraction TerrainInteraction;
        public int BaseRange;
    }

    [Serializable]
    public class JobData
    {
        public int JobId;
        public string JobName;
        public int DefaultMovementId;
        public int SecondaryMovementId;

        public List<int> JobPassiveEffects = new();

        public List<JobDataEntry> JobRewards;
        public List<JobPromotionEntry> JobPromotions;
    }

    public class UnitJob
    {
        public int UnitId;
        public int CurrentJobId;
        public int CurrentMovementId;
        public List<int> CurrentJobPassiveEffects = new();

        public int GetBaseMoveRange(MovementProfileDatabase mDb)
        {
            var moveProfile = mDb.Get(CurrentMovementId);

            return moveProfile.BaseRange;
        }

        public bool SwitchMoveProfile(JobDatabase jDb)
        {
            var jobData = jDb.GetJob(CurrentJobId);

            if (jobData.SecondaryMovementId < 0)
                return false;

            bool isUsingDefault = CurrentMovementId == jobData.DefaultMovementId;

            CurrentMovementId = isUsingDefault
                ? jobData.SecondaryMovementId
                : jobData.DefaultMovementId;

            return true;
        }
    }

    public class JobManager
    {
        private JobDatabase jobDb;
        private MovementProfileDatabase movementDb;
        private Dictionary<int,UnitJob> jobRegistry = new();
        private BattleSystems bs;

        public void InitializeSystem(GameManager gameManager,BattleSystems battleSystems)
        {
            jobDb = gameManager.JobDb;
            movementDb = gameManager.MovementProfileDb;
            bs = battleSystems;
        }

        public void RegisterUnit(int unitId, int startingJobId)
        {
            if (jobRegistry.ContainsKey(unitId))
                return;

            var jobData = jobDb.GetJob(startingJobId);

            var unitJob = new UnitJob
            {
                UnitId = unitId,
                CurrentJobId = startingJobId,
                CurrentMovementId = jobData.DefaultMovementId,
                CurrentJobPassiveEffects = new List<int>(jobData.JobPassiveEffects)
            };

            jobRegistry[unitId] = unitJob;
            bs.StatManager.SetOtherStat(unitId, unitJob.GetBaseMoveRange(movementDb), OtherStats.Move);
        }


        public void SwitchUnitMoveProfile(int unitId)
        {
            if (!jobRegistry.TryGetValue(unitId, out var unitJob)) return;

            if (!unitJob.SwitchMoveProfile(jobDb)) return;
            bs.StatManager.SetOtherStat(unitId, unitJob.GetBaseMoveRange(movementDb),OtherStats.Move);
        }

        public MovementProfileData GetUnitCurrentMoveProfile (int unitId)
        {
            if (!jobRegistry.TryGetValue(unitId, out var unitJob)) return null;
            return movementDb.Get(unitJob.CurrentJobId);
        }
    }
}
