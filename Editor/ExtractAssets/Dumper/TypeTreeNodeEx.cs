using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AssetBundleBrowser.ExtractAssets
{
    public static class TypeTreeNodeEx
    {
        #region [delegate]
        public delegate string CppTypeConvertHandler(TypeTreeNode varNode, List<TypeTreeNode> varTreeNodes, out List<TypeTreeNode> varFieldClassName);
        #endregion

        #region [Fields]
        private static Dictionary<string, string> _CppType2CSharp = new Dictionary<string, string>()
        {
            { "SInt8","sbyte"},
            { "UInt8","byte"},
            { "char","byte"},
            { "short","short"},
            { "SInt16","short"},
            { "UInt16","ushort"},
            { "unsigned short","ushort"},
            { "int","int"},
            { "SInt32","int"},
            { "UInt32","uint"},
            { "unsigned int","uint"},
            { "Type*","uint"},
            { "long long","long"},
            { "SInt64","long"},
            { "UInt64","ulong"},
            { "unsigned long long","ulong"},
            { "FileSize","ulong"},
            { "float","float"},
            { "double","double"},
            { "bool","bool"},
            { "string","string"},
        };
        private static Dictionary<string, CppTypeConvertHandler> _CppTypeConvert = new Dictionary<string, CppTypeConvertHandler>
        {
            { "vector" , CppTypeConvert_vector },
            { "map" , CppTypeConvert_map },
        };
        #endregion

        #region [API]
        /// <summary>
        /// 解析Field的变量Type结构
        /// </summary>
        /// <param name="varNode"></param>
        /// <param name="varTreeNodes"></param>
        /// <param name="varFieldTypeNodes"></param>
        /// <returns></returns>
        public static string GetNodeCsharpTypeDes(this TypeTreeNode varNode, List<TypeTreeNode> varTreeNodes, out List<TypeTreeNode> varFieldTypeNodes)
        {
            if (_CppType2CSharp.TryGetValue(varNode.m_Type, out var tempTypeVal))
            {
                varFieldTypeNodes = new List<TypeTreeNode>() { varNode };
                return tempTypeVal;
            }

            if (_CppTypeConvert.TryGetValue(varNode.m_Type, out var tempConverter))
            {
                return tempConverter(varNode, varTreeNodes, out varFieldTypeNodes);
            }
            //Debug.LogWarningFormat("GetNodeCsharpTypeDes not support [{0}]", varNode.m_Type);
            varFieldTypeNodes = new List<TypeTreeNode>() { varNode };
            return varNode.m_Type;
        }
        public static bool IsVauleType(this TypeTreeNode varNode) => _CppType2CSharp.ContainsKey(varNode.m_Type);
        #endregion

        #region [Business]
        private static string CppTypeConvert_vector(TypeTreeNode varNode, List<TypeTreeNode> varTreeNodes, out List<TypeTreeNode> varFieldTypeNodes)
        {
            var tempIdx = varNode.m_Index;
            //check
            {
                Debug.Assert(varTreeNodes[tempIdx + 1].m_Name == "Array");
                Debug.Assert(varTreeNodes[tempIdx + 2].m_Name == "size");
                Debug.Assert(varTreeNodes[tempIdx + 3].m_Name == "data");
            }
            var tempFieldDes = varTreeNodes[tempIdx + 3].GetNodeCsharpTypeDes(varTreeNodes, out varFieldTypeNodes);
            return string.Format("List<{0}>", tempFieldDes);
        }
        private static string CppTypeConvert_map(TypeTreeNode varNode, List<TypeTreeNode> varTreeNodes, out List<TypeTreeNode> varFieldTypeNodes)
        {
            var tempKey = string.Empty;
            var tempVal = string.Empty;
            varFieldTypeNodes = new List<TypeTreeNode>();
            for (int i = varNode.m_Index + 1; i < varTreeNodes.Count; i++)
            {
                var tempNode = varTreeNodes[i];
                if (tempNode.m_Level <= varNode.m_Level) break;

                //TODO - 这里是不是可以用level来判断，简化代码;

                if (tempNode.m_Name == "first")
                {
                    tempKey = tempNode.GetNodeCsharpTypeDes(varTreeNodes, out var varKeyClsNodes);
                    varFieldTypeNodes.AddRange(varKeyClsNodes);
                }

                if (tempNode.m_Name == "second")
                {
                    tempVal = tempNode.GetNodeCsharpTypeDes(varTreeNodes, out var varValueClsNodes);
                    varFieldTypeNodes.AddRange(varValueClsNodes);
                    break;
                }
            }

            Debug.Assert(!string.IsNullOrEmpty(tempKey));
            Debug.Assert(!string.IsNullOrEmpty(tempVal));

            return string.Format("Dictionary<{0}, {1}>", tempKey, tempVal);
        }
        #endregion
    }
}