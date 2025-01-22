using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AssetBundleBrowser
{
    public static class MiscUtils
    {
        #region [Fields]
        private static HashSet<string> _ValidateExtension = new HashSet<string> { ".dll", ".cs", ".meta", ".js", ".boo" };
        private static HashSet<string> _AtomAssetExtension = new HashSet<string>
        { ".png", "jpg", ".psd", ".tga", ".exr",
          ".txt", ".bytes", "byte",
          ".so", ".a", ".jar",
          ".java", ".mm", ".cpp", ".c",
        };
        #endregion

        #region [API]
        public static void ClearManifestByPath(string varOutputPath)
        {
            try
            {
                var tempDireInfo = new DirectoryInfo(varOutputPath);
                var tempManifestFiles = tempDireInfo.GetFiles("*.manifest", SearchOption.AllDirectories);
                for (int i = 0; i < tempManifestFiles.Length; ++i)
                {
                    FileInfo tempFile = tempManifestFiles[i];
                    EditorUtility.DisplayProgressBar("ClearManifest", string.Format("Delete {0}", tempFile.Name), i / (float)tempManifestFiles.Length);

                    if (tempFile.IsReadOnly) tempFile.IsReadOnly = false;
                    File.Delete(tempFile.FullName);
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
        public static bool ExportBundleJson(string varOutputPath, IEnumerable<string> varBundleNames)
        {
            if (string.IsNullOrEmpty(varOutputPath)) return false;
            var tempCount = varBundleNames.Count();
            if (null == varBundleNames || tempCount == 0) return false;

            var tempBuilds = new List<AssetBundleBuild>();
            try
            {
                var tempIdx = 0;
                foreach (var tempBundleName in varBundleNames)
                {
                    if (EditorUtility.DisplayCancelableProgressBar("ExportBundleJson", string.Format("ExportBundle {0}", tempBundleName), tempIdx++ / (float)tempCount)) break;

                    var tempAssets = AssetDatabase.GetAssetPathsFromAssetBundle(tempBundleName);
                    tempBuilds.Add(new AssetBundleBuild() { assetBundleName = tempBundleName, assetNames = tempAssets.OrderBy(a => a).ToArray() });
                }
                tempBuilds = tempBuilds.OrderBy(a => a.assetBundleName).ToList();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            File.WriteAllText(varOutputPath, JsonFx.Json.JsonWriter.Serialize(tempBuilds));

            return true;
        }
        public static bool ValidateAsset(string varABName)
        {
            if (!varABName.StartsWith("Assets/")) return false;

            var tempExt = Path.GetExtension(varABName).ToLower();
            return !_ValidateExtension.Contains(tempExt);
        }
        public static bool IsAtomAsset(string pathName)
        {
            if (!pathName.StartsWith("Assets/")) return false;

            var tempExt = Path.GetExtension(pathName).ToLower();
            return _AtomAssetExtension.Contains(tempExt);
        }
        public static void SetAssetBundleNameAndVariant_UseFileIO(string varAssetPath, string varBundleName, string varVariantName)
        {
            if (!varAssetPath.StartsWith("Assets/")) return;

            var tempMetaPath = Path.Combine(Application.dataPath, varAssetPath.Substring("Assets/".Length)) + ".meta";
            var tempMetaStr = File.ReadAllText(tempMetaPath);

            var tempBundleMatch = Regex.Match(tempMetaStr, @"assetBundleName\: [ \S]*", RegexOptions.Multiline);
            if (tempBundleMatch.Success)
            {
                tempMetaStr = tempMetaStr.Replace(tempBundleMatch.Value, "assetBundleName: " + varBundleName.Replace(@"\", "/").ToLower());
            }

            var tempVariantMatch = Regex.Match(tempMetaStr, @"assetBundleVariant\: [ \S]*", RegexOptions.Multiline);
            if (tempVariantMatch.Success)
            {
                tempMetaStr = tempMetaStr.Replace(tempVariantMatch.Value, "assetBundleVariant: " + varVariantName.Replace(@"\", "/").ToLower());
            }

            if (tempBundleMatch.Success || tempVariantMatch.Success)
            {
                File.WriteAllText(tempMetaPath, tempMetaStr);
            }
        }
        public static void GetAssetBundleNameAndVariant_UseFileIO(string varAssetPath, out string varBundleName, out string varVariantName)
        {
            varBundleName = varVariantName = string.Empty;

            if (!varAssetPath.StartsWith("Assets/")) return;

            var tempMetaPath = Path.Combine(Application.dataPath, varAssetPath.Substring("Assets/".Length)) + ".meta";
            var tempMetaStr = File.ReadAllText(tempMetaPath);
            var tempBundleMatch = Regex.Match(tempMetaStr, @"assetBundleName\: ([ \S]*)", RegexOptions.Multiline);
            if (tempBundleMatch.Success)
            {
                varBundleName = tempBundleMatch.Groups[1].Value;
            }

            var tempVariantMatch = Regex.Match(tempMetaStr, @"assetBundleVariant\: ([ \S]*)", RegexOptions.Multiline);
            if (tempVariantMatch.Success)
            {
                varBundleName = tempVariantMatch.Groups[1].Value;
            }
        }
        #endregion

        #region [GUI]
        public static void GUISignWithTimestamp(Rect varPosition, string varTimestamp)
        {
            EditorGUILayout.Space();

            GUIStyle _creditsStyle = new GUIStyle();
            _creditsStyle.fontStyle = FontStyle.Italic;
            _creditsStyle.alignment = TextAnchor.MiddleCenter;
            _creditsStyle.normal.textColor = new Color(0, 0, 0, 0.5f);
            GUI.Label(new Rect(15, varPosition.height - 20, varPosition.width, 20), string.Format("by 354888562@qq.com update:{0}", varTimestamp), _creditsStyle);
        }
        #endregion
    }
}