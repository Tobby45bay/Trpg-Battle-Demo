using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Game.Systems.Job
{
    [CreateAssetMenu(menuName = "Job System/Database/Movement Profile Database")]
    public class MovementProfileDatabase : ScriptableObject
    {
        public List<MovementProfileData> Proflies;

        private  Dictionary<int, MovementProfileData> lookup;

        public void Initialize()
        {
            lookup = Proflies.ToDictionary(p => p.Id);
        }

        public MovementProfileData Get(int id)
        {
            return lookup[id];
        }
    }
}

