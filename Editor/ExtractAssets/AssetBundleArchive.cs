using System;
using System.IO;
using UnityEngine;
using System.Linq;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;

using UObject = UnityEngine.Object;

namespace AssetBundleBrowser.ExtractAssets
{
    public class AssetBundleArchive
    {
        [MenuItem("ExtractAssets/Test")]
        public static void ExtractAssets()
        {
            var tempFilePath = EditorUtility.OpenFilePanel("ExtractAssets", Path.Combine(Application.dataPath, "../"), "*");
            if (string.IsNullOrEmpty(tempFilePath)) return;
            TestABFile(tempFilePath);
        }

        [MenuItem("ExtractAssets/QuickTest")]
        public static void Quick_ExtractAssets()
        {
            TestABFile(Path.Combine(Application.streamingAssetsPath, "quardandtriangle-prefab"));
        }

        private static void TestABFile(string varFilePath)
        {
            var tempStream = File.OpenRead(varFilePath);
            var tempBinaryStream = new EndianBinaryReader(tempStream);
            var tempStorage = new ArchiveStorageHeader(tempBinaryStream);
            Debug.LogError(tempStorage.HeaderInfo);
            Debug.LogError(string.Join(",", tempStorage.BlocksInfo));
            Debug.LogError(string.Join(",", tempStorage.DirectoryInfo));

            //ObjDecode(tempStorage);
            GenTypeTreeCode(tempStorage);
        }

        private static void ObjDecode(ArchiveStorageHeader varStorage)
        {
            foreach (var item in varStorage.DirectoryInfo)
            {
                if (!item.IsSerializedFile()) continue;

                var tempReader = new EndianBinaryReader(item.Context);
                var tempSF = new SerializedFile().Parse(tempReader);

                foreach (var tempKvp in tempSF.ObjectMap)
                {
                    var tempObj = tempKvp.Value;
                    var tempType = tempSF.Types[tempObj.typeID];

                    var tempTreeNodes = tempType.mTypeTree.Nodes;

                    tempReader.Seek(tempObj.byteStart + tempSF.Header.DataOffset, SeekOrigin.Begin);
                    var tempDecodeType = tempType.classID.ToString();
                    var tempDeserializeAction = Type.GetType($"AssetBundleBrowser.ExtractAssets.{tempDecodeType}").GetMethod("Deserialize", BindingFlags.Public | BindingFlags.Static);
                    var tempDecodeObj = tempDeserializeAction.Invoke(null, new object[] { tempReader });
                    Debug.LogError(tempDecodeObj);

                }

            }
        }

        private static void GenTypeTreeCode(ArchiveStorageHeader varStorage)
        {
            var tempCalssSet = new Dictionary<string, TypeTree2Class>();
            foreach (var item in varStorage.DirectoryInfo)
            {
                if (!item.IsSerializedFile()) continue;

                var tempReader = new EndianBinaryReader(item.Context);
                var tempSF = new SerializedFile().Parse(tempReader);

                var tempObjMap = tempSF.ObjectMap;
                foreach (var tempKvp in tempObjMap)
                {
                    var tempIdx = tempKvp.Value.typeID;
                    var tempType = tempSF.Types[tempIdx];
                    var tempTreeNodes = tempType.mTypeTree.Nodes;

                    var tempClsInfos = TypeTree2Class.Convert(tempTreeNodes);
                    foreach (var tempClsInfo in tempClsInfos)
                    {
                        if (tempCalssSet.TryGetValue(tempClsInfo.ClassName, out var tempCache))
                        {
                            tempCache.VaildIfConflict(tempClsInfo);
                        }
                        else
                        {
                            tempCalssSet.Add(tempClsInfo.ClassName, tempClsInfo);
                        }
                    }
                }
            }

            var tempStr = string.Empty;
            tempStr += "using System.Collections.Generic;\n\n";
            tempStr += "namespace AssetBundleBrowser.ExtractAssets\n{";
            foreach (var item in tempCalssSet)
            {
                tempStr += item.Value.Serialized();
            }
            tempStr += "\n}";

            File.WriteAllText(Path.Combine(Application.dataPath, "AssetBundles-Browser/Editor/ExtractAssets/Dumper/TypetreeGenCode.cs"), tempStr);
        }

    }
}