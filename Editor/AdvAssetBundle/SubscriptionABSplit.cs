using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using SObject = System.Object;

namespace AssetBundleBrowser.AdvAssetBundle
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class SubscriptionABSplitAttribute : Attribute
    {
        #region [Construct]
        public SubscriptionABSplitAttribute()
        {

        }
        #endregion

        #region [API]
        public static List<AssetDsc> ABSplitInfo()
        {
            var tempVals = new List<AssetDsc>();
            var tempSubSplitType = typeof(SubscriptionABSplitAttribute);
            var tempReturnType = typeof(List<AssetDsc>);
            var tempAssemblys = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var tempAssembly in tempAssemblys)
            {
                var tempTypes = tempAssembly.GetExportedTypes();
                foreach (var tempType in tempTypes)
                {
                    var tempMethods = tempType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    foreach (var tempMethod in tempMethods)
                    {
                        if (tempMethod.ReturnType != tempReturnType) continue;
                        if (!tempMethod.IsDefined(tempSubSplitType, false)) continue;

                        var tempTypeVals = tempMethod.Invoke(null, null) as List<AssetDsc>;
                        if (tempTypeVals == null || tempTypeVals.Count == 0) continue;

                        tempVals.AddRange(tempTypeVals);
                    }
                }
            }
            return tempVals;
        }
        #endregion
    }

    public class SubscriptionABSplitAttribute_test
    {
        [SubscriptionABSplit]
        public static void API()
        {

        }

        [SubscriptionABSplit]
        public void API2()
        {

        }

        [SubscriptionABSplit]
        public static List<AssetDsc> API32()
        {
            var temPS = new List<AssetDsc>();
            temPS.Add(new AssetDsc("aaa"));
            temPS.Add(new AssetDsc("aaa"));
            return temPS;
        }

        [MenuItem("SubscriptionABSplitAttribute/examples")]
        public static void examples()
        {
            {
                var tempAB = AssetDatabase.GetAllAssetBundleNames();
                var tempBuilList = new List<AssetBundleBuild>();
                foreach (var item in tempAB)
                {
                    var tempS = AssetDatabase.GetAssetPathsFromAssetBundle(item);

                    var tempBuild = new AssetBundleBuild();
                    tempBuild.assetBundleName = item;
                    tempBuild.assetNames = tempS;
                    Debug.LogError($"[{item}] {string.Join(";", tempS)}");
                    tempBuilList.Add(tempBuild);
                }
                var tempManifest = BuildPipeline.BuildAssetBundles("AssetBundles/Android_1", tempBuilList.ToArray(), BuildAssetBundleOptions.None, BuildTarget.Android);
            }

            {
                var buildManifest = BuildPipeline.BuildAssetBundles("AssetBundles/Android", BuildAssetBundleOptions.None, BuildTarget.Android);
            }

            {
                var tempVal = SubscriptionABSplitAttribute.ABSplitInfo();
                Debug.LogError(tempVal.Count);

                var tempSet = new HashSet<AssetDsc>(tempVal);
                Debug.LogError(tempSet.Count);

                var tempS = new AssetDsc("s", "");
                var tempS2 = new AssetDsc("s");
                Debug.LogError(tempS == tempS2);
            }
        }
    }
}