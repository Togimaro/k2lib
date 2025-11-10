
namespace LouveSystems.K2.Lib
{
    using System.Collections.Generic;
    using System.IO;

    public class RegionAttackRegionTransform : RegionRelatedTransform
    {
        public int AttackingRegionIndex => actingRegionIndex;

        public override int DecisionCost => isExtendedAttack ? 0 : 1;

        public int targetRegionIndex;

        public bool isExtendedAttack;

        public RegionAttackRegionTransform() : base() { }

        public RegionAttackRegionTransform(int attackingRegionIndex, int targetRegionIndex, bool isExtendedAttack, byte owningRealm) : base(attackingRegionIndex, owningRealm)
        {
            this.targetRegionIndex = targetRegionIndex;
            this.isExtendedAttack = isExtendedAttack;
        }

        public override ETransformKind Kind => ETransformKind.RegionAttack;

        protected override void ReadInternal(BinaryReader from)
        {
            base.ReadInternal(from);
            targetRegionIndex = from.ReadInt32();
            isExtendedAttack = from.ReadBoolean();
        }

        protected override void WriteInternal(BinaryWriter into)
        {
            base.WriteInternal(into);
            into.Write(targetRegionIndex);
            into.Write(isExtendedAttack);
        }

        public override string ToString()
        {
            return $"Region attack {AttackingRegionIndex} => {targetRegionIndex}";
        }
    }
}