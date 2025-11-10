
namespace LouveSystems.K2.Lib
{
    using System.IO;

    public struct Realm : IBinarySerializableWithVersion
    {
        public int silverTreasury;
        public int availableDecisions;
        public bool isFavoured;
        public byte factionIndex;

        public bool isSubjugated;
        public byte subjugatedBy;

        public int totalSpentStatistics;

        public int GetHash()
        {
            return Extensions.Hash(
                silverTreasury,
                availableDecisions,
                isFavoured ? 1 : 0,
                factionIndex
            );
        }

        public bool IsSubjugated(out byte subjugatingRealmIndex)
        {
            if (isSubjugated) {
                subjugatingRealmIndex = subjugatedBy;
                return true;
            }

            subjugatingRealmIndex = default;
            return false;
        }

        public void Read(byte version, BinaryReader from)
        {
            silverTreasury = from.ReadInt32();
            availableDecisions = from.ReadInt32();
            isFavoured = from.ReadBoolean();
            factionIndex = from.ReadByte();
        }

        public void Write(BinaryWriter into)
        {
            into.Write(silverTreasury);
            into.Write(availableDecisions);
            into.Write(isFavoured);
            into.Write(factionIndex);
        }

        public override string ToString()
        {
            return $"Realm [faction {factionIndex}] [{silverTreasury / 10f:n1} $] [favoured? {isFavoured}] [decisions: {availableDecisions}]";
        }
    }
}