
using System.Collections.Generic;
using TRPG.Core.Database;
using TRPG.Game.Systems.TRPGEfect;

namespace TRPG.Game.Const
{
    public class GamesDatabeses
    {
        public GameDatabase<EffectData> EffectDatabase { get; private set; }

        public void LoadEffectDatabase(List<EffectDataAuthor> effects)
        {
            var list = new List<EffectData>();

            foreach (EffectDataAuthor author in effects)
            {
                var data = author.Build();
                list.Add(data);
            }

            EffectDatabase = new GameDatabase<EffectData>(list);
        }
    }
}
