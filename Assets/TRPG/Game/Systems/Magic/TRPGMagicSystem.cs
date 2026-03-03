
namespace TRPG.Game.Systems.Magic
{
    public enum ElementType
    {
        Fire = 0,
        Water = 1,
        Wind = 2,
        Earth = 3,
        Light = 4,
        Dark = 5
    }

    public enum ElementAffinity
    {
        None = 0,
        Low = 1,
        High = 2
    }

    public sealed class MagicAffinity
    {
        public ElementType ElementType { get; }
        public ElementAffinity ElementAffinity { get; }

        public MagicAffinity(ElementType elementType, ElementAffinity elementAffinity)
        {
            ElementType = elementType;
            ElementAffinity = elementAffinity;
        }

        public override string ToString() => $"{ElementType} ({ElementAffinity})";
    }
}
