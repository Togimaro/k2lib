
namespace LouveSystems.K2.Lib
{
    using System.IO;

    public abstract class RegionRelatedTransform : Transform
    {
        public int actingRegionIndex;

        public RegionRelatedTransform() : base() { }

        public RegionRelatedTransform(int actingRegionIndex, byte owningRealm) : base(owningRealm)
        {
            this.actingRegionIndex = actingRegionIndex;
        }

        protected override void ReadInternal(BinaryReader from)
        {
            base.ReadInternal(from);
            actingRegionIndex = from.ReadInt32();
        }

        protected override void WriteInternal(BinaryWriter into)
        {
            base.WriteInternal(into);
            into.Write(actingRegionIndex);
        }
    }
}