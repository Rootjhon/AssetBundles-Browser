using System;
using System.Collections.Generic;

namespace AssetBundleBrowser.ExtractAssets
{
    public partial class ArchiveStorageHeader
    {
        /// <summary>
        /// Uncompressed archive file header (~46bytes).
        /// There might have the following layouts:
        /// header [ blocks directory ] [ data ] - Unity3d/UnityArchive
        /// header[data][blocks directory] - Unity3d/UnityArchive
        /// header blocks[directory data] - UnityRaw/UnityWeb - solid compression
        /// Directory lives inside the data blocks(compressed or not)
        /// </summary>
        public class Header
        {
            #region [Fields]
            /// <summary>
            /// String signature of the archive file.
            /// </summary>
            public string signature;
            /// <summary>
            /// Archive version.
            /// </summary>
            public uint version;
            /// <summary>
            /// Unity bundle version.
            /// </summary>
            public string unityWebBundleVersion;
            /// <summary>
            /// The minimum required unity revision.
            /// </summary>
            public string unityWebMinimumRevision;

            /// <summary>
            /// Total size of the compressed archive (header + blocks/directory + data).
            /// </summary>
            public long size;
            /// <summary>
            /// Size of the information about compressed blocks (and directory if it comes with blocks info)
            /// </summary>
            public uint compressedBlocksInfoSize;
            /// <summary>
            /// Size of the information about uncompressed blocks (and directory if it comes with blocks info)
            /// </summary>
            public uint uncompressedBlocksInfoSize;
            /// <summary>
            /// Archive flags (see ArchiveFlags enum)
            /// </summary>
            public uint flags;
            #endregion

            #region [API]
            public static Header Parse(EndianBinaryReader varReader)
            {
                var tempHead = new Header();
                tempHead.signature = varReader.ReadStringToNull();
                tempHead.version = varReader.ReadUInt32();
                tempHead.unityWebBundleVersion = varReader.ReadStringToNull();
                tempHead.unityWebMinimumRevision = varReader.ReadStringToNull();

                tempHead.size = varReader.ReadInt64();
                tempHead.compressedBlocksInfoSize = varReader.ReadUInt32();
                tempHead.uncompressedBlocksInfoSize = varReader.ReadUInt32();
                tempHead.flags = varReader.ReadUInt32();
                return tempHead;
            }

            public Compression.CompressionType GetBlocksInfoCompressionType()
            {
                var tempTypeVal = (int)(flags & (int)ArchiveFlags.kArchiveCompressionTypeMask);
                if (!Enum.IsDefined(typeof(Compression.CompressionType), tempTypeVal))
                {
                    throw new NotSupportedException($"[{tempTypeVal}] NotIsDefined CompressionType.");
                }
                return (Compression.CompressionType)tempTypeVal;
            }
            public void SetBlocksInfoCompressionType(uint compression)
            {
                flags = (flags & ~(uint)ArchiveFlags.kArchiveCompressionTypeMask) | (compression & (uint)ArchiveFlags.kArchiveCompressionTypeMask);
            }
            public bool HasBlocksAndDirectoryInfoCombined() => (flags & (uint)ArchiveFlags.kArchiveBlocksAndDirectoryInfoCombined) != 0;
            public void SetBlocksAndDirectoryInfoCombined(bool v)
            {
                flags = (flags & ~(uint)ArchiveFlags.kArchiveBlocksAndDirectoryInfoCombined) | (v ? (uint)ArchiveFlags.kArchiveBlocksAndDirectoryInfoCombined : 0);
            }
            public bool HasBlocksInfoAtTheEnd() => (flags & (uint)ArchiveFlags.kArchiveBlocksInfoAtTheEnd) != 0;
            public void SetBlocksInfoAtTheEnd(bool v)
            {
                flags = (flags & ~(uint)ArchiveFlags.kArchiveBlocksInfoAtTheEnd) | (v ? (uint)ArchiveFlags.kArchiveBlocksInfoAtTheEnd : 0);
            }
            #endregion

            #region [Override]
            public override string ToString()
            {
                var tempFlagStrs = new List<string>();
                foreach (var item in Enum.GetValues(typeof(ArchiveFlags)))
                {
                    var tempFlag = (ArchiveFlags)item;
                    if ((flags & (uint)tempFlag) == 0) continue;
                    tempFlagStrs.Add(tempFlag.ToString());
                }

                return $"signature:[{signature}] version:[{version}] unityWebBundleVersion:[{unityWebBundleVersion}] unityWebMinimumRevision:[{unityWebMinimumRevision}]" +
                    $" size:[{size}] compressedBlocksInfoSize:[{compressedBlocksInfoSize}] uncompressedBlocksInfoSize:[{uncompressedBlocksInfoSize}]" +
                    $" flags:[{flags}({string.Join(" | ", tempFlagStrs)})]";
            }
            #endregion
        }
    }
}