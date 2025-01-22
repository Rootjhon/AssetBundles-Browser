using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AssetBundleBrowser.ExtractAssets
{
    public partial class SerializedFile
    {
        public class ObjectInfo
        {
            #region [Fields]
            /// <summary>
            /// LocalIdentifierInFile
            /// </summary>
            public long LocalPathID;
            /// <summary>
            /// Need objectInfo.byteStart += header.m_DataOffset;;
            /// </summary>
            public long byteStart;
            public uint byteSize;
            /// <summary>
            /// Index of SerializedFile.Types;
            /// </summary>
            public int typeID;
            #endregion

            #region [API]
            public ObjectInfo Parse(EndianBinaryReader varStream)
            {
                varStream.AlignStream();
                LocalPathID = varStream.ReadInt64();

                byteStart = varStream.ReadInt64();

                //varStream.Seek(4,System.IO.SeekOrigin.Current);

                byteSize = varStream.ReadUInt32();
                typeID = varStream.ReadInt32();

                return this;
            }
            #endregion
        }
    }
}