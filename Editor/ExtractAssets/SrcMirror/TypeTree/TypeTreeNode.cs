using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AssetBundleBrowser.ExtractAssets
{
    /// <summary>
    /// What is this: Defines a node that make up TypeTree's data.
    /// Notes:
    ///     - This struct is serialized in blob so change to it actually always breaks the serialization of TypeTrees.
    ///  See BlobRead()/BlobWrite().
    /// </summary>
    public class TypeTreeNode
    {
        #region [Enum]
        public enum ETypeFlags : int
        {
            kFlagNone = 0,
            kFlagIsArray = (1 << 0),
            kFlagIsManagedReference = (1 << 1),
            kFlagIsManagedReferenceRegistry = (1 << 2),
            kFlagIsArrayOfRefs = (1 << 3)
        }
        /// <summary>
        /// Meta flags can be used like this:
        /// transfer.Transfer (someVar, "varname", kHideInEditorMask);
        /// The GenerateTypeTreeTransfer for example reads the metaflag mask and stores it in the TypeTree
        /// </summary>
        public enum TransferMetaFlags : int
        {
            kNoTransferFlags = 0,

            /// <summary>
            /// Putting this mask in a transfer will make the variable be hidden in the property editor
            /// </summary>
            kHideInEditorMask = 1 << 0,

            /// <summary>
            /// Makes a variable not editable in the property editor
            /// </summary>
            kNotEditableMask = 1 << 4,

            /// <summary>
            /// Makes arrays reorderable in the property editor
            /// </summary>
            kReorderable = 1 << 5,

            /// <summary>
            /// There are 3 types of PPtrs: kStrongPPtrMask, default (weak pointer)
            /// a Strong PPtr forces the referenced object to be cloned.
            /// A Weak PPtr doesnt clone the referenced object, but if the referenced object is being cloned anyway (eg. If another (strong) pptr references this object)
            /// this PPtr will be remapped to the cloned object
            /// If an  object  referenced by a WeakPPtr is not cloned, it will stay the same when duplicating and cloning, but be NULLed when templating
            /// </summary>
            kStrongPPtrMask = 1 << 6,

            // unused  = 1 << 7,

            /// <summary>
            /// kTreatIntegerValueAsBoolean makes an integer variable appear as a checkbox in the editor, be written as true/false to JSON, etc
            /// </summary>
            kTreatIntegerValueAsBoolean = 1 << 8,

            // unused = 1 << 9,
            // unused = 1 << 10,
            // unused = 1 << 11,

            /// <summary>
            /// When the options of a serializer tells you to serialize debug properties kSerializeDebugProperties
            /// All debug properties have to be marked kDebugPropertyMask
            /// Debug properties are shown in expert mode in the inspector but are not serialized normally
            /// </summary>
            kDebugPropertyMask = 1 << 12,

            /// <summary>
            /// Used in TypeTree to indicate that a property is aligned to a 4-byte boundary. Do not specify this flag when
            /// transferring variables; call transfer.Align() instead.
            /// </summary>
            kAlignBytesFlag = 1 << 14,

            /// <summary>
            /// Used in TypeTree to indicate that some child of this typetree node uses kAlignBytesFlag. Do not use this flag.
            /// </summary>
            kAnyChildUsesAlignBytesFlag = 1 << 15,

            // unused = 1 << 16,
            // unused = 1 << 18,

            /// <summary>
            /// Ignore this property when reading or writing .meta files
            /// </summary>
            kIgnoreInMetaFiles = 1 << 19,

            /// <summary>
            /// When reading meta files and this property is not present, read array entry name instead (for backwards compatibility).
            /// </summary>
            kTransferAsArrayEntryNameInMetaFiles = 1 << 20,

            /// <summary>
            /// When writing YAML Files, uses the flow mapping style (all properties in one line, with "{}").
            /// </summary>
            kTransferUsingFlowMappingStyle = 1 << 21,

            /// <summary>
            /// Tells SerializedProperty to generate bitwise difference information for this field.
            /// </summary>
            kGenerateBitwiseDifferences = 1 << 22,

            /// <summary>
            /// Makes a variable not be exposed to the animation system
            /// </summary>
            kDontAnimate = 1 << 23,

            /// <summary>
            /// Encodes a 64-bit signed or unsigned integer as a hex string in text serializers.
            /// </summary>
            kTransferHex64 = 1 << 24,

            /// <summary>
            /// Use to differentiate between uint16 and C# Char.
            /// </summary>
            kCharPropertyMask = 1 << 25,

            /// <summary>
            ///do not check if string is utf8 valid, (usually all string must be valid utf string, but sometimes we serialize pure binary data to string,
            ///for example TextAsset files with extension .bytes. In this case this validation should be turned off)
            ///Player builds will never validate data. In editor we validate correct encoding of strings by default.
            /// </summary>
            kDontValidateUTF8 = 1 << 26,

            /// <summary>
            /// Fixed buffers are serialized as arrays, use this flag to differentiate between regular arrays and fixed buffers.
            /// </summary>
            kFixedBufferFlag = 1 << 27,

            /// <summary>
            /// It is not allowed to modify this property's serialization data.
            /// </summary>
            kDisallowSerializedPropertyModification = 1 << 28
        }
        #endregion

        #region [Fields]
        /// <summary>
        /// The version of the serialization format as represented by this type tree.  Usually determined by Transfer() functions.
        /// </summary>
        public ushort m_Version;
        /// <summary>
        /// Level in the hierarchy (0 is the root)
        /// </summary>
        public byte m_Level;
        /// <summary>
        /// Possible values see ETypeFlags
        /// </summary>
        public byte m_TypeFlags;

        /// <summary>
        /// The type of the variable (eg. "Vector3f", "int")
        /// </summary>
        public uint m_TypeStrOffset;
        /// <summary>
        /// The name of the property (eg. "m_LocalPosition")
        /// </summary>
        public uint m_NameStrOffset;
        /// <summary>
        /// = -1 if its not determinable (arrays)
        /// </summary>
        public int m_ByteSize;
        /// <summary>
        /// The index of the property (Prefabs use this index in the override bitset);
        /// And Index of TypeTree Offset
        /// </summary>
        public int m_Index;

        /// <summary>
        /// Serialization meta data (eg. to hide variables in the property editor)
        /// Children or their meta flags with their parents!
        /// See TransferMetaFlags
        /// </summary>
        public int m_MetaFlag;

        /// <summary>
        /// When node is private reference, this holds the 64bit "hash" of the TypeTreeShareableData of the refed type.
        /// stores Hash128::PackToUInt64(). Why? because the Hash128 type is to expensive to initialize cpu wise(memset)
        /// 0 <=> does not reference a type.
        /// note: if this is deamed to much data (tends to always be zero), we could move the hash to TypeTreeShareableData as a vector and just keep a byte index here.
        /// </summary>
        public ulong m_RefTypeHash;


        public string m_Type;
        public string m_Name;
        #endregion

        #region [API]
        public TypeTreeNode Parse(EndianBinaryReader varStream, SerializedFileFormatVersion varFormat)
        {
            m_Version = varStream.ReadUInt16();
            m_Level = varStream.ReadByte();
            m_TypeFlags = varStream.ReadByte();
            m_TypeStrOffset = varStream.ReadUInt32();
            m_NameStrOffset = varStream.ReadUInt32();
            m_ByteSize = varStream.ReadInt32();
            m_Index = varStream.ReadInt32();
            m_MetaFlag = varStream.ReadInt32();
            if (varFormat >= SerializedFileFormatVersion.kTypeTreeNodeWithTypeFlags)
            {
                m_RefTypeHash = varStream.ReadUInt64();
            }
            return this;
        }

        public bool IsArray() => (m_TypeFlags & (int)ETypeFlags.kFlagIsArray) != 0;
        public bool IsManagedReference() => (m_TypeFlags & (int)ETypeFlags.kFlagIsManagedReference) != 0;
        public bool IsManagedReferenceRegistry() => (m_TypeFlags & (int)ETypeFlags.kFlagIsManagedReferenceRegistry) != 0;
        public bool IsArrayOfRefs() => (m_TypeFlags & (int)ETypeFlags.kFlagIsArrayOfRefs) != 0;
        #endregion

        #region [Override]
        public override string ToString()
        {
            return $"m_Version:[{m_Version}] m_Level:[{m_Level}] m_TypeFlags:[{m_TypeFlags}] m_TypeStrOffset:[{m_TypeStrOffset}]" +
                $"m_NameStrOffset:[{m_NameStrOffset}] m_ByteSize:[{m_ByteSize}] m_Index:[{m_Index}] m_MetaFlag:[{m_MetaFlag}]" +
                $"m_RefTypeHash:[{m_RefTypeHash}] m_Type:[{m_Type}] m_Name:[{m_Name}]";
        }
        #endregion
    }
}