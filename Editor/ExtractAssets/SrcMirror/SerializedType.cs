using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AssetBundleBrowser.ExtractAssets
{
    public class SerializedType
    {
        #region [Fields]
        public PersistentTypeID classID;
        public bool IsStrippedType;
        public short ScriptTypeIndex = -1;
        public TypeTree mTypeTree;
        /// <summary>
        /// Hash generated from assembly name, namespace, and class name, only available for script.<Hash128>
        /// </summary>
        public byte[] m_ScriptID;
        /// <summary>
        /// Old type tree hash.<Hash128>
        /// </summary>
        public byte[] m_OldTypeHash;
        /// <summary>
        /// When a SerializedType instance is describing a UnityEngine.Object type:
        /// This is the collection of all the types that where found to be referenced by fields that are marked as [SerializeRefence].
        /// (the scope of the list if the whole file and not limited to a single entry in the file)
        /// </summary>
        public int[] m_TypeDependencies;

        /// <summary>
        /// Only used for Referenced types
        /// </summary>
        public string m_KlassName;
        /// <summary>
        /// Only used for Referenced types
        /// </summary>
        public string m_NameSpace;
        /// <summary>
        /// Only used for Referenced types
        /// </summary>
        public string m_AsmName;
        #endregion

        #region [API]
        public SerializedType Parse(EndianBinaryReader varStream, bool varEnableTypeTree, SerializedFileFormatVersion varFormat, bool varRefType = false)
        {
            classID = (PersistentTypeID)varStream.ReadInt32();
            IsStrippedType = varStream.ReadBoolean();
            ScriptTypeIndex = varStream.ReadInt16();

            if (classID == PersistentTypeID.MonoBehaviour)
            {
                m_ScriptID = varStream.ReadBytes(16);
            }
            m_OldTypeHash = varStream.ReadBytes(16);

            if (varEnableTypeTree)
            {
                mTypeTree = new TypeTree().Parse(varStream, varFormat);

                if (varFormat >= SerializedFileFormatVersion.kStoresTypeDependencies)
                {
                    if (varRefType)
                    {
                        m_KlassName = varStream.ReadStringToNull();
                        m_NameSpace = varStream.ReadStringToNull();
                        m_AsmName = varStream.ReadStringToNull();
                    }
                    else
                    {
                        m_TypeDependencies = varStream.ReadInt32Array();
                    }
                }
            }
            return this;
        }
        #endregion

        #region [Override]
        public override string ToString()
        {
            return $"classID:[{classID}] IsStrippedType:[{IsStrippedType}] ScriptTypeIndex:[{ScriptTypeIndex}] mTypeTree:[{mTypeTree}] m_ScriptID:[{m_ScriptID}] " +
                $"m_OldTypeHash:[{string.Join("", m_OldTypeHash)}] m_TypeDependencies:[{string.Join("", m_TypeDependencies)}]" +
                $"m_KlassName:[{m_KlassName}] m_NameSpace:[{m_NameSpace}] m_AsmName:[{m_AsmName}]";
        }
        #endregion
    }
}