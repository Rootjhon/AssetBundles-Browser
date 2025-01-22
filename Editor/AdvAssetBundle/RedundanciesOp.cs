using System;
using System.Text;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;

using UDebug = UnityEngine.Debug;

namespace AssetBundleBrowser.AdvAssetBundle
{
    public static class RedundanciesOp
    {
        #region [Fields]
        private const string AutoGenABFormat = "autogen/{0}";
        private static IAssetDataQuerier _DefalutQuerier = new AssetDatabaseQuerier();
        private static readonly List<string> _EmptyList = new List<string>();
        #endregion

        #region [API]
        public static IAssetDataQuerier Optimize(ref string[] varAssetBundles) => Optimize(_DefalutQuerier, ref varAssetBundles);
        public static IAssetDataQuerier Optimize(ref AssetBundleBuild[] varABBuild)
        {
            var tempAssetBundles = varABBuild.Select(b => b.assetBundleName).ToArray();
            var tempABBuildQuerier = new AssetBundleBuildQuerier(varABBuild, varABBuild.Length);
            Optimize(tempABBuildQuerier, ref tempAssetBundles);
            return tempABBuildQuerier;
        }
        public static IAssetDataQuerier Optimize(IAssetDataQuerier varQuerier, ref string[] varAssetBundles)
        {
            using (new UDebugTimeTick("Sort all AssetBundle Names"))
            {
                var tempSortHelper = new List<string>(varAssetBundles);
                tempSortHelper.Sort();
                varAssetBundles = tempSortHelper.ToArray();
            }

            //K = AssetPath,V = Been Dep of AB;
            var tempAssetBeDeps = new Dictionary<string, List<string>>();
            //K = AB,V = Been Dep of Key;
            var tempABBeDep = new Dictionary<string, List<string>>();
            //K = AB,V = Key's Deps;
            var tempABDepABs = new Dictionary<string, List<string>>(varAssetBundles.Length);
            using (new UDebugTimeTick("Collecting AssetBundle Depends info."))
            {
                CollectABDepInfos(varQuerier, varAssetBundles, tempAssetBeDeps, tempABBeDep, tempABDepABs);
            }
            using (new UDebugTimeTick("Simple AB Depends"))
            {
                CurtailABDepend(tempAssetBeDeps, tempABBeDep);
            }

            using (new UDebugTimeTick("Sort Cache Info"))
            {
                foreach (var tempKvp in tempAssetBeDeps) tempKvp.Value.Sort();
                foreach (var tempKvp in tempABBeDep) tempKvp.Value.Sort();
                foreach (var tempKvp in tempABDepABs) tempKvp.Value.Sort();
            }

            var tempRepeatAssets = new List<string>();
            using (new UDebugTimeTick("Collecting repeat be depended for Asset"))
            {
                foreach (var tempKvp in tempAssetBeDeps)
                {
                    var tempAssetPath = tempKvp.Key;
                    if (tempKvp.Value.Count > 1)
                    {
                        tempRepeatAssets.Add(tempAssetPath);
                    }
                    else if (tempKvp.Value.Count == 1)
                    {
                        varQuerier.SetAssetBundleName(tempAssetPath, tempKvp.Value[0]);
                    }
                }
                tempRepeatAssets.Sort();
            }

            using (new UDebugTimeTick("Push same Depend AB to existing AB"))
            {
                PushSameDependABToABUnit(varQuerier, tempRepeatAssets, tempAssetBeDeps, tempABBeDep, tempABDepABs);
            }

            using (new UDebugTimeTick("Push Repeat Assets to AB or Creat AB Unit"))
            {
                PushSameAssetToABUnit(varQuerier, tempRepeatAssets, tempAssetBeDeps);
            }

            return varQuerier;
        }
        #endregion

        #region [Business]
        private static void CollectABDepInfos(IAssetDataQuerier varQuerier, string[] varAssetBundles,
            Dictionary<string, List<string>> varAssetBeDeps, Dictionary<string, List<string>> varABBeDep, Dictionary<string, List<string>> varABDepABs)
        {
            Action<string, string> tempRecordAssetBeDep = delegate (string varPath, string varAB)
            {
                if (varAssetBeDeps.TryGetValue(varPath, out var tempVal))
                {
                    tempVal.Add(varAB);
                }
                else
                {
                    varAssetBeDeps.Add(varPath, new List<string>() { varAB });
                }
            };
            Action<string, string> tempRecordABBeDep = delegate (string varDep, string varAB)
            {
                if (varABBeDep.TryGetValue(varDep, out var tempVal))
                {
                    tempVal.Add(varAB);
                }
                else
                {
                    varABBeDep.Add(varDep, new List<string>() { varAB });
                }
            };
            foreach (var tempABName in varAssetBundles)
            {
                var tempAssets = varQuerier.GetAssetPathsFromAssetBundle(tempABName);
                var tempAssetsDeps = varQuerier.GetDependencies(tempAssets, true);
                for (int iD = 0; iD < tempAssetsDeps.Length; ++iD)
                {
                    var tempDepPath = tempAssetsDeps[iD];
                    if (!MiscUtils.ValidateAsset(tempDepPath)) continue;

                    var tempDepABName = varQuerier.GetAssetBundleName(tempDepPath);
                    if (!string.IsNullOrEmpty(tempDepABName)) continue;

                    tempRecordAssetBeDep(tempDepPath, tempABName);
                }

                var tempABDeps = varQuerier.GetAssetBundleDependencies(tempABName, true);
                foreach (var tempABDep in tempABDeps)
                {
                    tempRecordABBeDep(tempABDep, tempABName);
                }

                varABDepABs.Add(tempABName, tempABDeps.Length == 0 ? _EmptyList : new List<string>(tempABDeps));
            }
        }
        private static void CurtailABDepend(Dictionary<string, List<string>> varAssetBeDep, Dictionary<string, List<string>> varABBeDep)
        {
            var tempSingleDep = new List<string>();
            foreach (var tempKvp in varAssetBeDep)
            {
                if (tempKvp.Value.Count < 2)
                {
                    tempSingleDep.Add(tempKvp.Key);
                    continue;
                }

                if (varABBeDep.Count == 0) continue;

                //间接依赖该资源的AB;
                var tempSecondhandABs = new List<string>();
                var tempABs = tempKvp.Value;
                foreach (var tempAsserBeDepAB in tempABs)
                {
                    if (varABBeDep.TryGetValue(tempAsserBeDepAB, out var tempBeDepList))
                    {
                        tempSecondhandABs.AddRange(tempBeDepList);
                    }
                }
                //切断间接依赖：A->B \ A->C \ B->C，切断 A -> C 后 => A -> B -> C;化简 C 的被依赖关系;
                var tempSecondhandABSet = new HashSet<string>(tempSecondhandABs);
                for (int iBD = tempKvp.Value.Count - 1; iBD >= 0; iBD--)
                {
                    var tempBeDepAB = tempKvp.Value[iBD];
                    if (!tempSecondhandABSet.Contains(tempBeDepAB)) continue;
                    tempKvp.Value.RemoveAt(iBD);
                }
            }

            //剔除被依赖列表小于2的资源（小于2的不主动设置abName）
            foreach (var item in tempSingleDep)
            {
                varAssetBeDep.Remove(item);
            }
        }
        private static void PushSameDependABToABUnit(IAssetDataQuerier varQuerier, List<string> varRepeatAssets,
            Dictionary<string, List<string>> varAssetBeDep, Dictionary<string, List<string>> varABBeDep, Dictionary<string, List<string>> varABDepABs)
        {
            var tempHashSetHelper = new HashSet<string>();
            for (int iR = 0; iR < varRepeatAssets.Count; ++iR)
            {
                var tempAssetPath = varRepeatAssets[iR];

                var tempAssetBeDeps = varAssetBeDep[tempAssetPath];
                if (tempAssetBeDeps.Count < 2)
                {
                    UDebug.LogErrorFormat("Repeat analysis error.[{0}]", tempAssetPath);
                    iR++;
                    continue;
                }

                tempHashSetHelper.Clear();
                for (int iBD = 0; iBD < tempAssetBeDeps.Count; ++iBD)
                {
                    var tempABName = tempAssetBeDeps[iBD];
                    tempHashSetHelper.UnionWith(varABDepABs[tempABName]);
                    tempHashSetHelper.Add(tempABName);
                }
                //TODO - 这里应该多一层考虑 TypeTree的重复性的内存优化
                var tempAllABDeps = tempHashSetHelper.OrderBy(a => a).ToList();
                for (int iAD = tempAllABDeps.Count - 1; iAD >= 0; --iAD)
                {
                    var tempABName = tempAllABDeps[iAD];

                    if (!varABBeDep.TryGetValue(tempABName, out var tempBeDeps)) continue;

                    if (tempAssetBeDeps.Count != tempBeDeps.Count) continue;

                    bool tempSame = true;
                    for (int iBD = 0; iBD < tempAssetBeDeps.Count; ++iBD)
                    {
                        if (tempAssetBeDeps[iBD] == tempBeDeps[iBD]) continue;
                        tempSame = false;
                        break;
                    }

                    if (!tempSame) continue;

                    varQuerier.SetAssetBundleName(tempAssetPath, tempABName);
                    varRepeatAssets.RemoveAt(iR);
                    iR++;
                    break;
                }
            }
        }
        private static void PushSameAssetToABUnit(IAssetDataQuerier varQuerier, List<string> varRepeatAssets,
            Dictionary<string, List<string>> varAssetBeDep)
        {
            for (int iR = 0; iR < varRepeatAssets.Count;)
            {
                var tempAssetPath = varRepeatAssets[iR];

                var tempAssetBeDeps = varAssetBeDep[tempAssetPath];
                if (tempAssetBeDeps.Count < 2)
                {
                    UDebug.LogErrorFormat("Repeat analysis error.[{0}]", tempAssetPath);
                    iR++;
                    continue;
                }

                var tempSameDeps = new List<int>() { iR };
                for (int iR2 = iR + 1; iR2 < varRepeatAssets.Count; ++iR2)
                {
                    var tempAsetBenDeps2 = varAssetBeDep[varRepeatAssets[iR2]];
                    if (tempAsetBenDeps2.Count != tempAssetBeDeps.Count) continue;

                    bool tempSameDep = true;
                    for (int iC = 0; iC < tempAssetBeDeps.Count; ++iC)
                    {
                        if (tempAssetBeDeps[iC] == tempAsetBenDeps2[iC]) continue;
                        tempSameDep = false;
                        break;
                    }
                    if (tempSameDep)
                    {
                        tempSameDeps.Add(iR2);
                    }
                }

                var tempNewUnitName = string.Format(AutoGenABFormat, HashABUnitName(tempAssetBeDeps));
                for (int iSD = tempSameDeps.Count - 1; iSD >= 0; --iSD)
                {
                    var tempIdx = tempSameDeps[iSD];
                    var tempPath = varRepeatAssets[tempIdx];
                    varQuerier.SetAssetBundleName(tempPath, tempNewUnitName);
                    varRepeatAssets.RemoveAt(tempIdx);
                }
            }
        }
        private static string HashABUnitName(List<string> varVals)
        {
            varVals.Sort();
            var tempStrVal = string.Join("_", varVals);
            var tmpStringBuilder = new StringBuilder();
            using (MD5 md5 = new MD5CryptoServiceProvider())
            {
                byte[] tmpHashValue = md5.ComputeHash(Encoding.UTF8.GetBytes(tempStrVal));
                tmpStringBuilder.Length = 0;

                for (int i = 0; i < tmpHashValue.Length; ++i)
                {
                    tmpStringBuilder.Append(tmpHashValue[i].ToString("x2"));
                }
                tempStrVal = tmpStringBuilder.ToString();
            }
            return tempStrVal;
        }
        #endregion
    }
}