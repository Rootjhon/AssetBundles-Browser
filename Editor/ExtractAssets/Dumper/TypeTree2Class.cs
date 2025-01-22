using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace AssetBundleBrowser.ExtractAssets
{
    public class TypeTree2Class
    {
        #region [Fields]
        private const string _ListPrefix = "List<";
        private const string _DictionaryPrefix = "Dictionary<";
        private static Dictionary<string, string> _BinaryReaderAPIMap = new Dictionary<string, string>
        {
            { "sbyte","ReadSByte"},
            { "byte","ReadByte"},
            { "short","ReadInt16"},
            { "ushort","ReadUInt16"},
            { "int","ReadInt32"},
            { "uint","ReadUInt32"},
            { "long","ReadInt64"},
            { "ulong","ReadUInt64"},
            { "float","ReadSingle"},
            { "double","ReadDouble"},
            { "bool","ReadBoolean"},
            { "string","ReadAlignedString"},
        };

        /// <summary>
        /// K = Field Name, V = Field Type;
        /// </summary>
        private Dictionary<string, string> _FieldsInfo;
        public string ClassName;

        private HashSet<string> _AlignField;
        #endregion

        #region [Construct]
        public TypeTree2Class(string varClsName, Dictionary<string, string> varFields, HashSet<string> varAlignFields)
        {
            Debug.Assert(!string.IsNullOrEmpty(varClsName));
            Debug.Assert(varFields != null);
            ClassName = varClsName;
            _FieldsInfo = varFields;
            _AlignField = varAlignFields;
        }
        #endregion

        #region [API]
        public static List<TypeTree2Class> Convert(List<TypeTreeNode> varTreeNodes) => Convert(varTreeNodes[0], varTreeNodes);
        public static List<TypeTree2Class> Convert(TypeTreeNode varFieldCls, List<TypeTreeNode> varTreeNodes)
        {
            PPtr.IsPPtr(varFieldCls.m_Type, out var tempClsName);
            if (string.IsNullOrEmpty(tempClsName)) tempClsName = varFieldCls.m_Type;

            var tempTreeClses = new List<TypeTree2Class>();

            var tempAlignFields = new HashSet<string>();
            var tempFieldNames = new Dictionary<string, string>();
            for (int iT = varFieldCls.m_Index + 1; iT < varTreeNodes.Count; ++iT)
            {
                var tempNode = varTreeNodes[iT];
                if (tempNode.m_Level <= varFieldCls.m_Level) break;
                if (tempNode.m_Level != varFieldCls.m_Level + 1) continue;

                var tempFieldName = CorrectFieldName(tempNode.m_Name);

                if ((tempNode.m_MetaFlag & (int)TypeTreeNode.TransferMetaFlags.kAlignBytesFlag) != 0)
                {
                    tempAlignFields.Add(tempFieldName);
                }

                tempFieldNames.Add(tempFieldName, tempNode.GetNodeCsharpTypeDes(varTreeNodes, out var tempFieldTypes));
                foreach (var item in tempFieldTypes)
                {
                    if (!item.IsVauleType())
                    {
                        tempTreeClses.AddRange(TypeTree2Class.Convert(item, varTreeNodes));
                    }
                }
            }

            tempTreeClses.Add(new TypeTree2Class(tempClsName, tempFieldNames, tempAlignFields));
            return tempTreeClses;
        }

        /// <summary>
        /// TODO - maybe need makesure the order the same,next version, for that maybe save with different version.;
        /// </summary>
        /// <param name="varVailder"></param>
        /// <returns></returns>
        public bool VaildIfConflict(TypeTree2Class varVailder)
        {
            if (varVailder.ClassName != this.ClassName) return false;

            if (varVailder._FieldsInfo.Count != this._FieldsInfo.Count) return true;

            foreach (var tempFieldsKvp in varVailder._FieldsInfo)
            {
                if (!this._FieldsInfo.TryGetValue(tempFieldsKvp.Key, out var tempFiledType))
                {
                    Debug.LogErrorFormat("Class Fields Decode [{0}] conflict in different TypeTree,Missing Field [{1}].", this.ClassName, tempFieldsKvp.Key);
                    return true;
                }
                else if (tempFiledType != tempFieldsKvp.Value)
                {
                    Debug.LogErrorFormat("Class Fields Decode [{0}] conflict in different TypeTree,Field [{1}] has different Type {2} vs {3}", this.ClassName, tempFieldsKvp.Key, tempFiledType, tempFieldsKvp.Value);
                    return true;
                }
            }

            return false;
        }
        public string Serialized()
        {
            var tempStrBuilder = new StringBuilder();
            tempStrBuilder.AppendFormat("public class {0}\n", ClassName);
            tempStrBuilder.AppendLine("{");
            {
                tempStrBuilder.AppendLine("#region [Fields]");
                {
                    var tempEnum = _FieldsInfo.GetEnumerator();
                    for (int iF = 0; iF < _FieldsInfo.Count; ++iF)
                    {
                        tempEnum.MoveNext();
                        var tempKvp = tempEnum.Current;

                        tempStrBuilder.AppendFormat("   /*{2}*/public {0} {1};\n", tempKvp.Value, tempKvp.Key, iF);
                    }
                }
                tempStrBuilder.AppendLine("#endregion");

                tempStrBuilder.AppendLine();

                tempStrBuilder.AppendLine("#region [Extractable]");
                {
                    tempStrBuilder.AppendFormat("public static {0} Deserialize(EndianBinaryReader varReader)\n", ClassName);
                    tempStrBuilder.AppendLine("{");
                    tempStrBuilder.AppendFormat("var tempItem = new {0}();\n", ClassName);
                    {
                        var tempEnum = _FieldsInfo.GetEnumerator();
                        for (int iF = 0; iF < _FieldsInfo.Count; ++iF)
                        {
                            tempEnum.MoveNext();
                            var tempKvp = tempEnum.Current;

                            tempStrBuilder.AppendLine(SerializedField($"tempItem.{tempKvp.Key}", tempKvp.Key, tempKvp.Value));
                        }
                    }
                    tempStrBuilder.AppendLine("return tempItem;\n}");

                    tempStrBuilder.AppendFormat("public {0} Serialize() => this;\n", ClassName);
                }
                tempStrBuilder.AppendLine("#endregion");
            }
            tempStrBuilder.Append("}\n");

            return tempStrBuilder.ToString();
        }
        #endregion

        #region [Business]
        public static string CorrectFieldName(string varFieldName)
        {
            var tempMatchs = Regex.Matches(varFieldName, "\\[(\\w+)\\]");
            for (int iM = 0; iM < tempMatchs.Count; ++iM)
            {
                var tempMatch = tempMatchs[iM];
                if (!tempMatch.Success) continue;

                varFieldName = varFieldName.Replace(tempMatch.Groups[0].Value, "_" + tempMatch.Groups[1].Value);
            }

            return varFieldName;
        }

        private string SerializedField(string varFieldName, string varMetaFieldName, string varFieldType)
        {
            varFieldType = varFieldType.Trim();

            string tempDecodeStr;
            if (_BinaryReaderAPIMap.TryGetValue(varFieldType, out tempDecodeStr))
            {
                tempDecodeStr = $"{varFieldName} = varReader.{tempDecodeStr}();";
                return WhetherAligned(varMetaFieldName, ref tempDecodeStr);
            }

            if (SerializedField_List(varFieldName, varFieldType, out tempDecodeStr))
            {
                return WhetherAligned(varMetaFieldName, ref tempDecodeStr);
            }

            if (SerializedField_Dic(varFieldName, varFieldType, out tempDecodeStr))
            {
                return WhetherAligned(varMetaFieldName, ref tempDecodeStr);
            }

            tempDecodeStr = $"{varFieldName} = {varFieldType}.Deserialize(varReader);";
            return WhetherAligned(varMetaFieldName, ref tempDecodeStr);
        }
        private bool SerializedField_List(string varFieldName, string varFieldType, out string varDecodeStr)
        {
            varDecodeStr = string.Empty;
            if (!varFieldType.StartsWith(_ListPrefix)) return false;

            var tempCodeLines = new List<string>();
            tempCodeLines.Add("{");
            tempCodeLines.Add($"{varFieldName} = new {varFieldType}();");

            var tempID = varFieldType.Length;
            var tempType = varFieldType.Substring(_ListPrefix.Length, varFieldType.Length - _ListPrefix.Length - 1);
            tempCodeLines.Add($"var tempSize_{tempID} = varReader.ReadInt32();");
            tempCodeLines.Add($"for (int i_{tempID} = 0; i_{tempID} < tempSize_{tempID}; ++i_{tempID})");
            tempCodeLines.Add("{");
            tempCodeLines.Add($"{varFieldName}.Add(default);");
            tempCodeLines.Add(SerializedField($"{varFieldName}[i_{tempID}]", null, tempType));
            tempCodeLines.Add("}");

            tempCodeLines.Add("}");
            varDecodeStr = string.Join("\n", tempCodeLines);
            return true;
        }
        private bool SerializedField_Dic(string varFieldName, string varFieldType, out string varDecodeStr)
        {
            varDecodeStr = string.Empty;
            if (!varFieldType.StartsWith(_DictionaryPrefix)) return false;

            var tempCodeLines = new List<string>();
            tempCodeLines.Add("{");
            tempCodeLines.Add($"{varFieldName} = new {varFieldType}();");

            var tempID = varFieldType.Length;
            var tempTypes = varFieldType.Substring(_DictionaryPrefix.Length, varFieldType.Length - _DictionaryPrefix.Length - 1).Split(',');
            var tempKeyType = tempTypes[0];
            var tempValType = tempTypes[1];

            tempCodeLines.Add($"var tempSize_{tempID} = varReader.ReadInt32();");
            tempCodeLines.Add($"for (int i_{tempID} = 0; i_{tempID} < tempSize_{tempID}; ++i_{tempID})");
            {
                tempCodeLines.Add("{");
                {
                    tempCodeLines.Add($"{tempKeyType} tempKey_{tempID};");
                    {
                        tempCodeLines.Add("{");
                        tempCodeLines.Add(SerializedField($"tempKey_{tempID}", null, tempKeyType));
                        tempCodeLines.Add("}");
                    }

                    tempCodeLines.Add($"{tempValType} tempVal_{tempID};");
                    {
                        tempCodeLines.Add("{");
                        tempCodeLines.Add(SerializedField($"tempVal_{tempID}", null, tempValType));
                        tempCodeLines.Add("}");
                    }
                    tempCodeLines.Add($"{varFieldName}.Add(tempKey_{tempID}, tempVal_{tempID});");
                }
                tempCodeLines.Add("}");
            }

            tempCodeLines.Add("}");
            varDecodeStr = string.Join("\n", tempCodeLines);
            return true;
        }
        private string WhetherAligned(string varMetaFieldName, ref string varDecodeStr)
        {
            if (_AlignField.Contains(varMetaFieldName))
            {
                varDecodeStr += "\nvarReader.AlignStream();";
            }
            return varDecodeStr;
        }
        #endregion
    }
}