using System;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

namespace AssetBundleBrowser.AdvAssetBundle
{
    public class AssetBundleBuildQuerier : IAssetDataQuerier
    {
        #region [Fields]
        private string[] _EmptyStrArray = new string[] { };
        /// <summary>
        /// Key = ABName;
        /// </summary>
        private Dictionary<string, AssetBundleBuild> _BuildABMap;
        /// <summary>
        /// key = assetPath,Val = ABName;
        /// </summary>
        private Dictionary<string, string> _AssetABName;
        #endregion

        #region [Construct]
        public AssetBundleBuildQuerier(IEnumerable<AssetBundleBuild> varABBuild, int varCount)
        {
            _AssetABName = new Dictionary<string, string>();
            _BuildABMap = new Dictionary<string, AssetBundleBuild>(varCount);
            foreach (var item in varABBuild)
            {
                var tempABName = item.assetBundleName;
                if (string.IsNullOrEmpty(tempABName)) continue;
                if (item.assetNames == null || item.assetNames.Length == 0) continue;

                if (_BuildABMap.TryGetValue(tempABName, out var tempBuild))
                {
                    if (tempBuild.Equals(item)) continue;

                    var tempCachingAstStr = string.Join(";", tempBuild.assetNames);
                    var tempNewAstStr = string.Join(";", item.assetNames);
                    throw new Exception($"[Conflict] Same AssetBundleName [{tempABName}] been signed with different assetNames,see deatil below:\n[1]:[{tempCachingAstStr}]\n[2][{tempNewAstStr}]");
                }
                _BuildABMap.Add(tempABName, item);
            }
        }
        #endregion

        #region [IAssetDataQuerier]
        public string[] GetAssetBundleDependencies(string assetBundleName, bool recursive)
        {
            if (!_BuildABMap.TryGetValue(assetBundleName, out var tempBuild)) return _EmptyStrArray;

            var tempDepBundles = new HashSet<string>();
            var tempAstNames = this.GetDependencies(tempBuild.assetNames, recursive);
            foreach (var tempAst in tempAstNames)
            {
                if (!MiscUtils.ValidateAsset(tempAst)) continue;

                var tempAstABName = this.GetAssetBundleName(tempAst);
                if (string.IsNullOrEmpty(tempAstABName)) continue;
                if (tempAstABName == assetBundleName) continue;

                tempDepBundles.Add(tempAstABName);
            }

            if (tempDepBundles.Count == 0) return _EmptyStrArray;

            var tempArray = new string[tempDepBundles.Count];
            tempDepBundles.CopyTo(tempArray);
            return tempArray;
        }

        public string GetAssetBundleName(string path)
        {
            foreach (var tempKvp in _BuildABMap)
            {
                if (tempKvp.Value.assetNames.Contains(path))
                {
                    return tempKvp.Key;
                }
            }
            return string.Empty;
        }

        public string[] GetAssetPathsFromAssetBundle(string assetBundleName)
        {
            if (_BuildABMap.TryGetValue(assetBundleName, out var tempBuild))
            {
                return tempBuild.assetNames;
            }
            return _EmptyStrArray;
        }

        public virtual string[] GetDependencies(string[] pathNames, bool recursive)
            => AssetDatabase.GetDependencies(pathNames, recursive);

        public void SetAssetBundleName(string path, string assetBundleName)
        {
            if (_AssetABName.ContainsKey(path))
            {
                _AssetABName[path] = assetBundleName;
            }
            else
            {
                _AssetABName.Add(path, assetBundleName);
            }
        }

        public AssetBundleBuild[] OptimizedAssetBundleInfo()
        {
            //K = ABName,V = AssetPaths;
            var tempABAsts = new Dictionary<string, HashSet<string>>();
            foreach (var tempKvp in _AssetABName)
            {
                if (tempABAsts.TryGetValue(tempKvp.Value, out var tempVal))
                {
                    tempVal.Add(tempKvp.Key);
                }
                else
                {
                    tempABAsts.Add(tempKvp.Value, new HashSet<string>() { tempKvp.Key });
                }
            }
            var tempValidBuilds = new Dictionary<string, AssetBundleBuild>();
            foreach (var tempKvp in _BuildABMap)
            {
                var tempBuild = tempKvp.Value;
                var tempAssets = tempBuild.assetNames;
                bool tempValid = true;
                var tempInvalid = new HashSet<string>();
                foreach (var item in tempAssets)
                {
                    if (!_AssetABName.ContainsKey(item)) continue;
                    tempInvalid.Add(item);
                    tempValid = false;
                }
                if (!tempValid)
                {
                    var tempList = new HashSet<string>(tempAssets);
                    foreach (var item in tempInvalid)
                    {
                        tempList.Remove(item);
                    }
                    tempBuild.assetNames = tempList.ToArray();
                    tempValidBuilds.Add(tempKvp.Key, tempBuild);
                }
            }
            foreach (var tempKvp in tempValidBuilds)
            {
                _BuildABMap[tempKvp.Key] = tempKvp.Value;
            }

            var tempABInfos = new List<AssetBundleBuild>();
            foreach (var tempKvp in _BuildABMap)
            {
                var tempBuild = tempKvp.Value;
                if (tempABAsts.TryGetValue(tempKvp.Key, out var tempVal))
                {
                    var tempAstPaths = new HashSet<string>(tempBuild.assetNames);
                    foreach (var item in tempVal)
                    {
                        tempAstPaths.Add(item);
                    }
                    tempBuild.assetNames = tempAstPaths.OrderBy(a => a).ToArray();

                    tempABAsts.Remove(tempKvp.Key);
                }

                tempABInfos.Add(tempBuild);
            }

            foreach (var tempKvp in tempABAsts)
            {
                tempABInfos.Add(new AssetBundleBuild() { assetBundleName = tempKvp.Key.ToLower(), assetNames = tempKvp.Value.OrderBy(a => a).ToArray() });
            }

            return tempABInfos.OrderBy(b => b.assetBundleName).ToArray();
        }
        #endregion
    }
}