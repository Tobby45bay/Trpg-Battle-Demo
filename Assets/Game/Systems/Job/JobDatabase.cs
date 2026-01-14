using Game.Systems.Effect;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Game.Core.Game.GameManager.GameConst;
namespace Game.Systems.Job
{
    [CreateAssetMenu(menuName = "Job System/Database/Job Database")]
    public class JobDatabase : ScriptableObject
    {
        // Start is called before the first frame update
        public List<JobDataOS> JobDataList;
        private Dictionary<int,JobData> lookupTable;

        public void Initialize()
        {
            lookupTable = new Dictionary<int, JobData>();
            foreach (var j in JobDataList)
                lookupTable[j.JobData.JobId] = j.JobData;
        }

        public JobData GetJob(int id)
        {
            if (lookupTable == null) Initialize();
            return lookupTable.TryGetValue(id, out var j) ? j : null;
        }
    }
}
