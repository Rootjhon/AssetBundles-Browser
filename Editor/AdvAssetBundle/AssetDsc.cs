using System;

namespace AssetBundleBrowser.AdvAssetBundle
{
    public sealed class AssetDsc
    {
        #region [Fields]
        public string AssetsGUID;
        public string AssetBundleName { get; private set; }
        #endregion

        #region [Construct]
        public AssetDsc(string varGUID) { AssetsGUID = varGUID; }
        public AssetDsc(string varGUID, string varAssetBundleName) { AssetsGUID = varGUID; AssetBundleName = varAssetBundleName.ToLower(); }
        #endregion

        #region [API]
        public AssetDsc TagAssetBundleName(string varAssetBundleName)
        {
            if (string.IsNullOrEmpty(varAssetBundleName)) throw new ArgumentException("Can't tag NullOrEmpty string to AssetBundle.");
            AssetBundleName = varAssetBundleName.ToLower();
            return this;
        }
        #endregion

        #region [Override]
        public override int GetHashCode() => AssetsGUID.GetHashCode();
        public override bool Equals(object varObj) => this == (varObj as AssetDsc);
        public override string ToString() => string.Format("GUID[{0}]-[{1}]", AssetsGUID, AssetBundleName);
        public static bool operator !=(AssetDsc lhs, AssetDsc rhs) => !(lhs == rhs);
        public static bool operator ==(AssetDsc lhs, AssetDsc rhs)
        {
            if (lhs is null) return rhs is null;
            if (rhs is null) return false;
            return lhs.AssetsGUID == rhs.AssetsGUID && lhs.AssetBundleName == rhs.AssetBundleName;
        }
        #endregion
    }
}