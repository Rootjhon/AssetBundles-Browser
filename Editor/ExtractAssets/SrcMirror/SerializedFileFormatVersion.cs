namespace AssetBundleBrowser.ExtractAssets
{
    public enum SerializedFileFormatVersion : uint
    {
        /// <summary>
        /// format no longer readable.
        /// </summary>
        kUnsupported = 1,
        /// <summary>
        /// semantic lost to history, but tested against in code.
        /// </summary>
        kUnknown_2 = 2,
        /// <summary>
        /// semantic lost to history, but tested against in code.
        /// </summary>
        kUnknown_3 = 3,
        /// <summary>
        /// semantic lost to history, but tested against in code.
        /// </summary>
        kUnknown_5 = 5,
        /// <summary>
        /// semantic lost to history, but tested against in code.
        /// </summary>
        kUnknown_6 = 6,
        /// <summary>
        /// semantic lost to history, but tested against in code.
        /// </summary>
        kUnknown_7 = 7,
        /// <summary>
        /// semantic lost to history, but tested against in code.
        /// </summary>
        kUnknown_8 = 8,
        /// <summary>
        /// semantic lost to history, but tested against in code.
        /// </summary>
        kUnknown_9 = 9,
        /// <summary>
        /// Developed in parallel: Version 10 Blobified TypeTree
        /// </summary>
        kUnknown_10 = 10,
        /// <summary>
        /// Developed in parallel: Version 11 Script References
        /// </summary>
        kHasScriptTypeIndex = 11,
        /// <summary>
        /// Version: 12  Blobified TypeTree & Script References
        /// </summary>
        kUnknown_12 = 12,
        /// <summary>
        ///  kHasTypeTreeHashes
        /// </summary>
        kHasTypeTreeHashes = 13,
        /// <summary>
        /// semantic lost to history, but tested against in code.
        /// </summary>
        kUnknown_14 = 14,
        /// <summary>
        /// kSupportsStrippedObject
        /// </summary>
        kSupportsStrippedObject = 15,
        /// <summary>
        /// 5.5: widened serialized ClassID to 32 bit.
        /// </summary>
        kRefactoredClassId = 16,
        /// <summary>
        /// 5.5: moved all other type-data from Object to Type
        /// </summary>
        kRefactorTypeData = 17,
        /// <summary>
        /// 2019.1 : TypeTree's now reference a shareable/cachable data set
        /// </summary>
        kRefactorShareableTypeTreeData = 18,
        /// <summary>
        /// 2019.1: TypeTree's can contain nodes that express managed references
        /// </summary>
        kTypeTreeNodeWithTypeFlags = 19,
        /// <summary>
        /// 2019.2: SerializeFile support managed references
        /// </summary>
        kSupportsRefObject = 20,
        /// <summary>
        /// 2019.2: SerializeFile includes info on types that depend on other types
        /// </summary>
        kStoresTypeDependencies = 21,
        /// <summary>
        /// 2020.1: Large file support
        /// </summary>
        kLargeFilesSupport = 22,

        /// <summary>
        /// increment when changing the serialization format and add an enum above for previous version logic checks
        /// </summary>
        //kCurrentSerializeVersion = kLargeFilesSupport,
    }
}

