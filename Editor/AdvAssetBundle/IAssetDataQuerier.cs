using UnityEditor;

namespace AssetBundleBrowser.AdvAssetBundle
{
    public interface IAssetDataQuerier
    {
        /// <summary>
        /// Get the AssetBundle name.
        /// </summary>
        /// <param name="path">Path are relative to the project folder, for example: "Assets/MyTextures/hello.png".</param>
        /// <returns></returns>
        string GetAssetBundleName(string path);
        /// <summary>
        /// Set the AssetBundle name.
        /// </summary>
        /// <param name="path">Path are relative to the project folder, for example: "Assets/MyTextures/hello.png".</param>
        /// <param name="assetBundleName"></param>
        void SetAssetBundleName(string path, string assetBundleName);
        /// <summary>
        /// Given a pathName, returns the list of all assets that it depends on.
        /// </summary>
        /// <param name="pathNames">The path to the asset for which dependencies are required.</param>
        /// <param name="recursive">If false, return only assets which are direct dependencies of the input; if true, include all indirect dependencies of the input, also include the input path itself.</param>
        /// <returns></returns>
        string[] GetDependencies(string[] pathNames, bool recursive);
        /// <summary>
        /// Get the paths of the assets which have been marked with the given assetBundle name.
        /// All paths are relative to the project folder, for example: "Assets/MyTextures/hello.png".
        /// </summary>
        /// <param name="assetBundleName"></param>
        /// <returns></returns>
        string[] GetAssetPathsFromAssetBundle(string assetBundleName);
        /// <summary>
        /// Given an assetBundleName, returns the list of AssetBundles that it depends on.
        /// </summary>
        /// <param name="assetBundleName">The name of the AssetBundle for which dependencies are required.</param>
        /// <param name="recursive">If false, returns only AssetBundles which are direct dependencies of the input; if true, includes all indirect dependencies of the input.</param>
        /// <returns>The names of all AssetBundles that the input depends on.</returns>
        string[] GetAssetBundleDependencies(string assetBundleName, bool recursive);
        AssetBundleBuild[] OptimizedAssetBundleInfo();
    }
}