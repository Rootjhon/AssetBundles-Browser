using UnityEditor;
using System.Linq;

namespace AssetBundleBrowser.AdvAssetBundle
{
    public sealed class AssetDatabaseQuerier : IAssetDataQuerier
    {
        #region [IAssetDataQuerier]
        public string[] GetAssetBundleDependencies(string assetBundleName, bool recursive) 
            => AssetDatabase.GetAssetBundleDependencies(assetBundleName, recursive);

        public string GetAssetBundleName(string path)
            => AssetImporter.GetAtPath(path).assetBundleName;

        public string[] GetAssetPathsFromAssetBundle(string assetBundleName)
            => AssetDatabase.GetAssetPathsFromAssetBundle(assetBundleName.ToLower());

        public string[] GetDependencies(string[] pathNames, bool recursive)
            => AssetDatabase.GetDependencies(pathNames, recursive);

        public void SetAssetBundleName(string path, string assetBundleName)
            => AssetImporter.GetAtPath(path).SetAssetBundleNameAndVariant(assetBundleName.ToLower(), string.Empty);
        public AssetBundleBuild[] OptimizedAssetBundleInfo()
        {
            var tempABs = AssetDatabase.GetAllAssetBundleNames();
            var tempBuilds = new AssetBundleBuild[tempABs.Length];
            for (int iB = 0; iB < tempABs.Length; ++iB)
            {
                var tempABName = tempABs[iB];
                tempBuilds[iB] = new AssetBundleBuild()
                {
                    assetBundleName = tempABName,
                    assetNames = this.GetAssetPathsFromAssetBundle(tempABName).OrderBy(n => n).ToArray()
                };
            }
            return tempBuilds.OrderBy(b => b.assetBundleName).ToArray();
        }
        #endregion
    }
}