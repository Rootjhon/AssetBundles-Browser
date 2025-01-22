using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace AssetBundleBrowser.ExtractAssets
{
    public enum EndianType : int
    {
        LittleEndian = 0,
        BigEndian
    }

    public class EndianBinaryReader : BinaryReader
    {
        #region [Fields]
        public EndianType endian;
        public long Position
        {
            get => BaseStream.Position;
            set => BaseStream.Position = value;
        }
        #endregion

        #region [Construct]
        public EndianBinaryReader(Stream stream, EndianType endian = EndianType.BigEndian) : base(stream)
        {
            this.endian = endian;
        }
        #endregion

        #region [API]
        public override short ReadInt16()
        {
            if (endian == EndianType.BigEndian)
            {
                var buff = ReadBytes(2);
                Array.Reverse(buff);
                return BitConverter.ToInt16(buff, 0);
            }
            return base.ReadInt16();
        }

        public override int ReadInt32()
        {
            if (endian == EndianType.BigEndian)
            {
                var buff = ReadBytes(4);
                Array.Reverse(buff);
                return BitConverter.ToInt32(buff, 0);
            }
            return base.ReadInt32();
        }

        public override long ReadInt64()
        {
            if (endian == EndianType.BigEndian)
            {
                var buff = ReadBytes(8);
                Array.Reverse(buff);
                return BitConverter.ToInt64(buff, 0);
            }
            return base.ReadInt64();
        }

        public override ushort ReadUInt16()
        {
            if (endian == EndianType.BigEndian)
            {
                var buff = ReadBytes(2);
                Array.Reverse(buff);
                return BitConverter.ToUInt16(buff, 0);
            }
            return base.ReadUInt16();
        }

        public override uint ReadUInt32()
        {
            if (endian == EndianType.BigEndian)
            {
                var buff = ReadBytes(4);
                Array.Reverse(buff);
                return BitConverter.ToUInt32(buff, 0);
            }
            return base.ReadUInt32();
        }

        public override ulong ReadUInt64()
        {
            if (endian == EndianType.BigEndian)
            {
                var buff = ReadBytes(8);
                Array.Reverse(buff);
                return BitConverter.ToUInt64(buff, 0);
            }
            return base.ReadUInt64();
        }

        public override float ReadSingle()
        {
            if (endian == EndianType.BigEndian)
            {
                var buff = ReadBytes(4);
                Array.Reverse(buff);
                return BitConverter.ToSingle(buff, 0);
            }
            return base.ReadSingle();
        }

        public override double ReadDouble()
        {
            if (endian == EndianType.BigEndian)
            {
                var buff = ReadBytes(8);
                Array.Reverse(buff);
                return BitConverter.ToUInt64(buff, 0);
            }
            return base.ReadDouble();
        }

        public long Seek(long offset, SeekOrigin origin)
        {
            return BaseStream.Seek(offset, origin);
        }
        #endregion
    }
}