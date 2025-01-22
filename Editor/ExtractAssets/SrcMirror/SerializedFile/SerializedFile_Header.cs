using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AssetBundleBrowser.ExtractAssets
{
    public partial class SerializedFile
    {
        /// <summary>
        /// This header is always in BigEndian when in file Metadata follows directly after the header
        /// </summary>
        public class SerializedFileHeader
        {
            #region [Fields]
            public const int kHeaderSize_Ver8 = 12;

            public uint MetadataSize;
            public long FileSize;
            public SerializedFileFormatVersion Version;
            public long DataOffset;
            public byte Endianess;
            public byte[] Reserved = new byte[3];
            #endregion

            #region [API]
            public SerializedFileHeader Parse(EndianBinaryReader varStream)
            {
                MetadataSize = varStream.ReadUInt32();
                FileSize = varStream.ReadUInt32();
                Version = (SerializedFileFormatVersion)varStream.ReadUInt32();

                {
                    DataOffset = varStream.ReadUInt32();

                    Endianess = varStream.ReadByte();
                    Reserved = varStream.ReadBytes(3);

                    MetadataSize = varStream.ReadUInt32();
                    FileSize = varStream.ReadInt64();
                    DataOffset = varStream.ReadInt64();
                    varStream.ReadInt64(); // unknown
                }

                return this;

                //if (Version < SerializedFileFormatVersion.kLargeFilesSupport)
                //{
                //    DataOffset = varStream.ReadUInt32();
                //    Endianess = varStream.ReadByte();
                //    Reserved = varStream.ReadBytes(Reserved.Length);
                //}
                //else
                //{
                //    varStream.Seek(4, SeekOrigin.Current);

                //    varStream.Seek(4, SeekOrigin.Current);
                //    MetadataSize = varStream.ReadUInt32();

                //    varStream.Seek(4, SeekOrigin.Current);
                //    FileSize = varStream.ReadUInt32();

                //    varStream.Seek(4, SeekOrigin.Current);
                //    DataOffset = varStream.ReadUInt32();

                //    varStream.Seek(4, SeekOrigin.Current);
                //    Endianess = varStream.ReadByte();
                //    Reserved = varStream.ReadBytes(Reserved.Length);
                //}

            }
            #endregion

            #region [Override]
            public override string ToString()
            {
                return $"MetadataSize:[{MetadataSize}] FileSize:[{FileSize}] Version:[{Version}] DataOffset:[{DataOffset}] Endianess:[{Endianess}] Reserved:[{string.Join("", Reserved)}]";
            }
            #endregion
        }
    }
}