using System;

namespace AssetBundleBrowser.ExtractAssets
{
    public class FileIdentifier
    {
        #region [Enum]
        public enum InsertMode { kDontCreate = 0, kCreate = 1, kAllowRemap = 2 };
        public enum FileIdentifierType : int
        {
            /// <summary>
            /// guid is valid if it's a const guid otherwise null, pathName can point to anywhere except in registered asset folders
            /// </summary>
            kNonAssetType = 0,
            kDeprecatedCachedAssetType = 1,
            /// <summary>
            /// guid is valid, pathName is empty, the resolved path is in a registered asset folder
            /// </summary>
            kSerializedAssetType = 2,
            /// <summary>
            /// guid is valid, pathName is empty, the resolved path is an artifact path
            /// </summary>
            kMetaAssetType = 3,
            kAssetTypeCount = 4
        };
        #endregion

        #region [Fields]
        public string pathName;
        public Guid guid;
        public FileIdentifierType type;
        #endregion

        public FileIdentifier Parse(EndianBinaryReader varStream)
        {
            var tempEmptyStr = varStream.ReadStringToNull();
            guid = new Guid(varStream.ReadBytes(16));
            type = (FileIdentifierType)varStream.ReadInt32();
            pathName = varStream.ReadStringToNull();
            return this;
        }

    }
}