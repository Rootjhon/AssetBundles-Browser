using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AssetBundleBrowser.ExtractAssets
{
    public class LocalSerializedObjectIdentifier
    {
        #region [Fields]
        public int localSerializedFileIndex;
        public long localIdentifierInFile;
        #endregion

        #region [API]
        public LocalSerializedObjectIdentifier Parse(EndianBinaryReader varStream)
        {
            localSerializedFileIndex = varStream.ReadInt32();
            varStream.AlignStream();
            localIdentifierInFile = varStream.ReadInt64();
            return this;
        }
        #endregion
    }
}