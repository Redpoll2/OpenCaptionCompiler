using System.IO;

namespace OpenCaptionCompiler.ClosedCaptions
{
    internal struct CaptionHeader
    {
        public int Signature;
        public int Version;
        public int BlockCount;
        public int BlockSize;
        public int DirectorySize;
        public int DataStart;

        public static CaptionHeader Parse(BinaryReader reader)
        {
            return new CaptionHeader
            {
                Signature = reader.ReadInt32(),
                Version = reader.ReadInt32(),
                BlockCount = reader.ReadInt32(),
                BlockSize = reader.ReadInt32(),
                DirectorySize = reader.ReadInt32(),
                DataStart = reader.ReadInt32(),
            };
        }
    }

    internal struct CaptionDirectoryEntry
    {
        public uint Hash;
        public int BlockId;
        public ushort DataStart;
        public ushort DataSize;

        public static CaptionDirectoryEntry Parse(BinaryReader reader)
        {
            return new CaptionDirectoryEntry
            {
                Hash = reader.ReadUInt32(),
                BlockId = reader.ReadInt32(),
                DataStart = reader.ReadUInt16(),
                DataSize = reader.ReadUInt16(),
            };
        }
    }
}
