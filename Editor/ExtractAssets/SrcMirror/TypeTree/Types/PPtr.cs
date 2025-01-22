namespace AssetBundleBrowser.ExtractAssets
{
    public class PPtr<T> : UnityEngine.Object
    {
        #region [Fields]
        public int m_FileID;
        public long m_PathID;
        #endregion

        #region [IExtractable]
        public static PPtr<T> Deserialize(EndianBinaryReader varReader)
        {
            var tempItem = new PPtr<T>();
            tempItem.m_FileID = varReader.ReadInt32();
            tempItem.m_PathID = varReader.ReadInt64();
            return tempItem;
        }
        public PPtr<T> Serialize() => null;
        #endregion
    }
}