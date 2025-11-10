namespace LouveSystems.K2.Lib
{
    using System;
    using System.IO;

    public interface IBinaryReadableVersionable
    {
        void Read(byte version, BinaryReader from);
    }

    public interface IBinaryReadable
    {
        void Read(BinaryReader from);
    }

    public interface IBinaryWriteable : IHashable
    {
        void Write(BinaryWriter into);
        int IHashable.GetHash()
        {
            using (MemoryStream ms = new MemoryStream()) {
                using (BinaryWriter bw = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true)) {
                    Write(bw);
                }

                int[] hashes = new int[ms.Length / 4 + 1];

                ms.Seek(0, SeekOrigin.Begin);
                for (int i = 0; i < hashes.Length; i++) {

                    byte[] buff = new byte[4];

                    ms.Read(buff, 0, buff.Length);

                    hashes[i] = BitConverter.ToInt32(buff);
                }

                return Extensions.Hash(hashes);
            }
        }
    }


    public interface IBinarySerializableWithVersion : IHashable, IBinaryWriteable, IBinaryReadableVersionable
    {
    }

    public interface IBinarySerializable : IHashable, IBinaryWriteable, IBinaryReadable
    {
    }
}